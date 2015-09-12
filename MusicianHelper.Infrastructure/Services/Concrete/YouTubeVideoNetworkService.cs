﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using Google.Apis.Json;
using Google.Apis.YouTube.v3.Data;
using log4net;
using MusicianHelper.Common.Helpers;
using MusicianHelper.Common.Helpers.Log;
using MusicianHelper.Infrastructure.Models;
using MusicianHelper.Infrastructure.Services.Abstract;
using Newtonsoft.Json;

namespace MusicianHelper.Infrastructure.Services.Concrete
{
    public class YouTubeVideoNetworkService : IVideoNetworkService
    {

        private static readonly ILog Log = LogHelper.GetLogger(typeof(YouTubeVideoNetworkService));

        private const string YOUTUBE_URL = "https://accounts.google.com/o/oauth2/auth?client_id=392497808537-83iin347g6ncno4ff0a3ufv0c353vtv5.apps.googleusercontent.com&redirect_uri=urn:ietf:wg:oauth:2.0:oob&response_type=code&scope=https://www.googleapis.com/auth/youtube.upload https://www.googleapis.com/auth/youtube";
        private const string OAUTH_URL = "https://accounts.google.com/o/oauth2/token";

        private readonly IStorageService _ss;

        private OauthTokenModel _otm = null;

        public YouTubeVideoNetworkService()
            : this(IoCHelper.Instance.GetService<IStorageService>())
        {

        }

        public YouTubeVideoNetworkService(IStorageService ss)
        {
            _ss = ss;
        }

        public string ExtractAuthToken(string returnString)
        {
            try
            {
                var parts = returnString.Split('=');
                return parts[1];
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return null;
            }
        }

        public Uri CreateRequestUri()
        {
            try
            {
                Uri uri;
                Uri.TryCreate(YOUTUBE_URL, UriKind.Absolute, out uri);
                return uri;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return null;
            }
        }

        public OauthTokenModel GetRequestTokens(string authToken)
        {
            try
            {
                if (string.IsNullOrEmpty(authToken))
                    return null;

                //code	The authorization code that Google returned to your redirect_uri in step 4.
                //client_id	The OAuth 2.0 client ID for your application. This value is displayed in the Google APIs console.
                //client_secret	The client secret associated with your client ID. This value is displayed in the Google APIs console.
                //redirect_uri	A registered redirect_uri for your client ID.
                //grant_type	Set this value to authorization_code.

                using (var wb = new WebClient())
                {
                    var data = new NameValueCollection();
                    data["code"] = authToken;
                    data["client_id"] = "392497808537-83iin347g6ncno4ff0a3ufv0c353vtv5.apps.googleusercontent.com";
                    data["client_secret"] = "KeSVYrRyJE7y-WXM_gsUVrY7";
                    data["redirect_uri"] = "urn:ietf:wg:oauth:2.0:oob";
                    data["grant_type"] = "authorization_code";

                    var response = wb.UploadValues(OAUTH_URL, "POST", data);
                    var responseStr = Encoding.ASCII.GetString(response);
                    var oauthResponse = JsonConvert.DeserializeObject<youtube_oauth_response>(responseStr);

                    return oauthResponse.ToOauthTokenModel();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return null;
            }
        }

        public void SaveOauthResponse(OauthTokenModel otm)
        {
            try
            {
                var sm = _ss.Load();

                sm.AccessToken = otm.AccessToken;
                sm.RefreshToken = otm.RefreshToken;

                _ss.Save(sm);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
        }

        public async void UploadVideo(string videoPath, OauthTokenModel otm)
        {
            try
            {
            //https://www.googleapis.com//upload/youtube/v3/videos?uploadType=resumable&part=snippet,status,contentDetails
                var bytes = File.ReadAllBytes(videoPath);

                Video returnedVideo = null;
                using (var stream = File.OpenRead(videoPath))
                {

                    var video = new Video
                    {
                        Snippet = new VideoSnippet
                        {
                            Title = "Default Video Title",
                            Description = "Default Video Description",
                            Tags = new string[] {"tag1", "tag2"},
                            //TODO get category list info
                            CategoryId = "22"
                        },
                        Status = new VideoStatus
                        {
                            PrivacyStatus = "unlisted",
                            Embeddable = true,
                            License = "youtube"
                        }
                    };

                    var headers = new Dictionary<string, string>();

                    headers["Authorization"] = "Bearer " + otm.AccessToken;
                    headers["X-Upload-Content-Length"] = bytes.Length.ToString();
                    headers["x-upload-content-type"] = "application/octet-stream";

                    IJsonSerializer js = new NewtonsoftJsonSerializer();
                    var videoData = js.Serialize(video);

                    var response =
                        await
                            WebHelper.GetRawResponsePost(
                                "https://www.googleapis.com/upload/youtube/v3/videos?uploadType=resumable&part=snippet,status,contentDetails",
                                videoData, headers);

                    var uploadUrl = response.Headers.Location;

                    returnedVideo = await WebHelper.Post<Video>(
                        uploadUrl.AbsoluteUri,
                        stream, headers);
                }

                var t = "t";

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
        }

        public OauthTokenModel GetOauthTokenModel()
        {
            try
            {
                return _otm ?? (_otm = _ss.Load().ToOauthTokenModel());
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return null;
            }
        }

    }
}

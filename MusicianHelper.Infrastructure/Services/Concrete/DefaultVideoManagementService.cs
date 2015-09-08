﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using log4net;
using MusicianHelper.Common.Helpers.Log;
using MusicianHelper.Infrastructure.Models;
using MusicianHelper.Infrastructure.Models.Enums;
using MusicianHelper.Infrastructure.Services.Abstract;

namespace MusicianHelper.Infrastructure.Services.Concrete
{
    public class DefaultVideoManagementService : IVideoManagementService
    {

        private static readonly ILog Log = LogHelper.GetLogger(typeof (DefaultVideoManagementService));

        private VideoManagmentSettings _vms = null;
        private readonly IVideoProcessingService _vps = null;

        public DefaultVideoManagementService(IVideoProcessingService vps)
        {
            _vps = vps;
        }

        private static string BaseDir
        {
            get
            {
                return Environment.CurrentDirectory;
            }
        }

        public string GetRenderDirectory()
        {
            var path = Path.Combine(BaseDir, _vms.RenderedVideoDirectory);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        public void CreateVideoFromImages(List<string> imagePaths, string audioPath, string outputFilename, EventHandler renderCompleted = null)
        {
            try
            {
                var ehList = new List<EventHandler>();

                if (renderCompleted != null)
                    ehList.Add(renderCompleted);

                ehList.Add(RenderCompleted);

                _vps.CreateVideoFromImages(imagePaths, audioPath, Path.Combine(GetRenderDirectory(), _vps.GetCorrectFilename(outputFilename)), VideoQuality.HD2_NTSC, ehList);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                throw;
            }
        }

        private void RenderCompleted(object sender, EventArgs e)
        {
            try
            {
                var t = "t";
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                throw;
            }
        }

        public void Configure(VideoManagmentSettings settings)
        {
            _vms = settings;
        }

    }
}

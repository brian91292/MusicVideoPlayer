﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MusicVideoPlayer.Util;
using System.Diagnostics;
using IllusionPlugin;
using System.Text.RegularExpressions;

namespace MusicVideoPlayer.YT
{
    public class YouTubeDownloader : MonoBehaviour
    {
        public event Action<VideoData> downloadProgress;

        public VideoQuality quality = VideoQuality.Medium;

        Queue<VideoData> videoQueue;
        bool downloading;

        Process ydl;

        private static YouTubeDownloader _instance = null;
        public static YouTubeDownloader Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = new GameObject("YoutubeDownloader").AddComponent<YouTubeDownloader>();
                    DontDestroyOnLoad(_instance);
                    _instance.videoQueue = new Queue<VideoData>();
                    _instance.quality = (VideoQuality)ModPrefs.GetInt(Plugin.PluginName, "VideoDownloadQuality", (int)VideoQuality.Medium, true);
                    _instance.downloading = false;
                }
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        public void EnqueueVideo(VideoData video)
        {
            videoQueue.Enqueue(video);
            if (!downloading)
            {
                video.downloadState = DownloadState.Queued;
                DownloadVideo();
            }
        }

        public void DequeueVideo(VideoData video)
        {
            video.downloadState = DownloadState.Cancelled;
        }

        private void DownloadVideo()
        {
            VideoData video = videoQueue.Peek();
            if (video.downloadState == DownloadState.Cancelled)
            {
                // skip
                videoQueue.Dequeue();

                if (videoQueue.Count > 0)
                {
                    // Start next download
                    DownloadVideo();
                }
                else
                {
                    // queue empty
                    downloading = false;
                    return;
                }
            }

            downloading = true;
            video.downloadState = DownloadState.Downloading;
            downloadProgress?.Invoke(video);
            string levelPath = VideoLoader.GetLevelPath(video.level);
            if (!Directory.Exists(levelPath)) Directory.CreateDirectory(levelPath);
            // Download the video via youtube-dl 
            ydl = new Process();

            string videoFileName = video.title;
            // Strip invalid characters
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                videoFileName = videoFileName.Replace(c, '-');
            }
            video.videoPath = videoFileName + ".mp4";

            ydl.StartInfo.FileName = Environment.CurrentDirectory + "/Youtube-dl/youtube-dl.exe";
            ydl.StartInfo.Arguments =
                "https://www.youtube.com" + video.URL +
                " -f \"" + VideoQualitySetting.Format(quality) + "\"" + // Formats
                " --no-cache-dir" + // Don't use temp storage
                " -o \"" + levelPath + $"\\{videoFileName}.%(ext)s\"" +
                " --no-playlist" +  // Don't download playlists, only the first video
                " --no-part";  // Don't store download in parts, write directly to file
            

            ydl.StartInfo.RedirectStandardOutput = true;
            ydl.StartInfo.RedirectStandardError = true;
            ydl.StartInfo.UseShellExecute = false;
            ydl.StartInfo.CreateNoWindow = true;
            ydl.EnableRaisingEvents = true;

            ydl.Start();

            // Hook up our output to console
            ydl.BeginOutputReadLine();
            ydl.BeginErrorReadLine();

            ydl.OutputDataReceived += (sender, e) => {
                if (e.Data != null)
                {
                    //[download]  81.8% of 40.55MiB at  4.80MiB/s ETA 00:01
                    //[download] Resuming download at byte 48809440
                    //
                    Regex rx = new Regex(@"(\d+).\d%+");
                    Match match = rx.Match(e.Data);
                    if (match.Success)
                    {
                        video.downloadProgress = float.Parse(match.Value.Substring(0, match.Value.Length - 2)) / 100;
                        downloadProgress?.Invoke(video);
                    }
                    Console.WriteLine(e.Data);
                }
            };

            ydl.ErrorDataReceived += (sender, e) => {
                Console.WriteLine(e.Data);
                //to do: check these errors problems - redownload or skip file when an error occurs
            };

            ydl.Exited += (sender, e) => {
                // to do: check that the file was indeed downloaded correctly
                
                if (video.downloadState == DownloadState.Cancelled)
                {
                    VideoLoader.Instance.DeleteVideo(video);
                }
                else
                {
                    video.downloadState = DownloadState.Downloaded;
                    VideoLoader.SaveVideoToDisk(video);
                    StartCoroutine(VerifyDownload(video));
                }

                videoQueue.Dequeue();

                if (videoQueue.Count > 0)
                {
                    // Start next download
                    DownloadVideo();                    
                }
                else
                {
                    // queue empty
                    downloading = false;
                }
            };
        }

        public void OnApplicationQuit()
        {
            ydl.Close(); // or .Kill()
            ydl.Dispose();
        }

        public VideoData GetDownloadingVideo(IBeatmapLevel level)
        {
            return videoQueue.FirstOrDefault(x => x.level == level);
        }

        private IEnumerator VerifyDownload(VideoData video)
        {
            yield return new WaitForSecondsRealtime(1);

            if (File.Exists(VideoLoader.Instance.GetVideoPath(video)))
            {
                // video okay?
                downloadProgress?.Invoke(video);
            }
            
        }

    }
}

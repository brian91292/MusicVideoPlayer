﻿using CustomUI.BeatSaber;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using MusicVideoPlayer.Util;
using MusicVideoPlayer.YT;

namespace MusicVideoPlayer.UI
{
    class VideoUI : MonoBehaviour
    {
        private static VideoUI _instance = null;
        public static VideoUI Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = new GameObject("VideoPlayerMenuTweaks").AddComponent<VideoUI>();
                    DontDestroyOnLoad(_instance.gameObject);
                }
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        IBeatmapLevel selectedLevel;

        private VideoFlowCoordinator _videoFlowCoordinator;

        private Button _videoButton;
        private Image _videoButtonGlow;
        private HoverHint _videoButtonHint;
        private BeatmapDifficultyViewController _difficultyViewController;

        private Image _progressCircle;

        public void OnLoad()
        {
            SetupTweaks();
        }

        private void SetupTweaks()
        {
            YouTubeDownloader.Instance.downloadProgress += VideoDownloaderDownloadProgress;

            _videoFlowCoordinator = gameObject.AddComponent<VideoFlowCoordinator>();

            _videoFlowCoordinator.finished += VideoFlowCoordinatorFinished;
            _videoFlowCoordinator.Init();

            _difficultyViewController = Resources.FindObjectsOfTypeAll<BeatmapDifficultyViewController>().FirstOrDefault();
            _difficultyViewController.didSelectDifficultyEvent += DifficultyViewControllerDidSelectDifficultyEvent;

            var _detailViewController = Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().First(x => x.name == "StandardLevelDetailViewController");

            RectTransform buttonsRect = _detailViewController.GetComponentsInChildren<RectTransform>().First(x => x.name == "Buttons");
            var _playbutton = buttonsRect.GetComponentsInChildren<Button>().First(x => x.name == "PlayButton");
            var _practiceButton = buttonsRect.GetComponentsInChildren<Button>().First(x => x.name == "PracticeButton");

            _videoButton = Instantiate(_practiceButton, buttonsRect.parent);
            _videoButton.name = "VideoButton";
            _videoButton.SetButtonIcon(Base64Sprites.PlayIcon);
            (_videoButton.transform as RectTransform).anchoredPosition = new Vector2(46, -6);
            (_videoButton.transform as RectTransform).sizeDelta = new Vector2(8, 8);
            _videoButton.onClick.AddListener(delegate () { _videoFlowCoordinator.Present(); });

            _videoButtonHint = BeatSaberUI.AddHintText(_videoButton.transform as RectTransform, "Download a video");

            var glow = _playbutton.GetComponentsInChildren<RectTransform>().First(x => x.name == "GlowContainer");
            var videoWrapper = _videoButton.GetComponentsInChildren<RectTransform>().First(x => x.name == "Wrapper");
            _videoButtonGlow = Instantiate(glow.gameObject, videoWrapper).gameObject.GetComponentInChildren<Image>();

            var hlg = _videoButton.GetComponentsInChildren<HorizontalLayoutGroup>().First(x => x.name == "Content");
            hlg.padding = new RectOffset(3, 2, 2, 2);

            _progressCircle = videoWrapper.GetComponentsInChildren<Image>().First(x => x.name == "Stroke");
            _progressCircle.type = Image.Type.Filled;
            _progressCircle.fillMethod = Image.FillMethod.Radial360;
            _progressCircle.fillAmount = 1f;

        }

        private void VideoFlowCoordinatorFinished(VideoData video)
        {
            UpdateVideoButton(video);
        }

        private void VideoDownloaderVideoDownloaded(VideoData video)
        {
            if (selectedLevel == video.level)
            {
                UpdateVideoButton(video);
            }
        }

        private void VideoDownloaderDownloadProgress(VideoData video)
        {
            if (selectedLevel == video.level)
            {
                UpdateVideoButton(video);
            }
        }

        private void DifficultyViewControllerDidSelectDifficultyEvent(BeatmapDifficultyViewController sender, IDifficultyBeatmap beatmap)
        {
            selectedLevel = beatmap.level;
            UpdateVideoButton(VideoLoader.Instance.GetVideo(selectedLevel));
        }

        private void UpdateVideoButton(VideoData selectedVideo)
        {
            //if (selectedLevel.levelID.Length >= 32)
            //{
            _videoButton.interactable = true;

            selectedVideo = VideoLoader.Instance.GetVideo(selectedLevel);

            if (selectedVideo != null)
            {
                if (selectedVideo.downloadState == DownloadState.Queued)
                {
                    // video queued

                }
                else if (selectedVideo.downloadState == DownloadState.Downloading)
                {
                    // video downloading
                    _videoButtonHint.text = String.Format("Downloading: {0:#.0}%", selectedVideo.downloadProgress * 100);
                    _videoButtonGlow.gameObject.SetActive(false);
                    _progressCircle.color = Color.Lerp(Color.red, Color.green, selectedVideo.downloadProgress);
                    _progressCircle.fillAmount = selectedVideo.downloadProgress;
                }
                else if (selectedVideo.downloadState == DownloadState.Downloaded)
                {
                    // video ready
                    _videoButtonGlow.gameObject.SetActive(true);
                    _videoButtonGlow.color = Color.green;
                    _videoButtonHint.text = "<color=#c0c0c0><size=80%>Replace existing video";
                    _progressCircle.fillAmount = 1f;
                    _progressCircle.color = new Color(0.75f, 0.75f, 0.75f);
                }
                else
                {
                    // notdownloaded, downloading or queued
                    _videoButtonGlow.gameObject.SetActive(false);
                    _videoButton.interactable = false;
                    _videoButtonHint.text = "<color=#808080><size=80%>Video unavailable :(";
                    _progressCircle.fillAmount = 1f;
                    _progressCircle.color = new Color(0.5f, 0.5f, 0.5f);
                }
            }
            else
            {
                // no video
                _videoButtonGlow.gameObject.SetActive(true);
                _videoButtonGlow.color = Color.red;
                _videoButtonHint.text = "Add a Video";
                _progressCircle.fillAmount = 1f;
                _progressCircle.color = Color.white;
            }
            //}
            //else
            //{
            //    // OST song - no videos supported yet
            //    _videoButtonGlow.gameObject.SetActive(false);
            //    _videoButton.interactable = false;
            //    _videoButtonHint.text = "<color=#808080><size=80%>Videos not available for OST songs\nsorry :(";
            //    _progressCircle.fillAmount = 1f;
            //    _progressCircle.color = new Color(0.5f, 0.5f, 0.5f);
            //}
        }
    }
}

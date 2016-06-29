﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Zedarus.ToolKit.Data;
using Zedarus.ToolKit.Settings;
using Zedarus.ToolKit.Events;

namespace Zedarus.ToolKit.API
{
	public class MediationAdsController : APIController 
	{	
		#region Events
		public event Action InterstitialClosed;
		public event Action GrantReward;
		public event Action BannerDisplayed;
		public event Action BannerRemoved;
		#endregion

		#region Properties
		private bool _interstitialsCached = false;
		private bool _rewardVideosCached = false;
		private Action _interstitialClosedCallback = null;
		#endregion

		#region Initialization
		protected override void Setup() { }
		#endregion

		#region Wrappers Initialization
		protected override IAPIWrapperInterface GetWrapperForAPI(int wrapperAPI)
		{
			switch (wrapperAPI)
			{
				case APIs.Ads.HeyZap:
					return HeyZapWrapper.Instance;
				default:
					return null;
			}
		}
		#endregion

		#region Controls - Caching
		public void CacheIntersitials(string tag, params string[] otherTags)
		{
			if (_interstitialsCached)
				return;

			IMediationAdsWrapperInterface wrapper = Wrapper;
			if (Enabled && wrapper != null)
			{
				wrapper.CacheInterstitial(tag);
				foreach (string otherTag in otherTags)
				{
					wrapper.CacheInterstitial(tag);
				}
			}

			_interstitialsCached = true;
		}

		public void CacheRewardVideos(string tag, params string[] otherTags)
		{
			if (_rewardVideosCached)
				return;

			IMediationAdsWrapperInterface wrapper = Wrapper;
			if (Enabled && wrapper != null)
			{
				wrapper.CacheRewardedVideo(tag);
				foreach (string otherTag in otherTags)
				{
					wrapper.CacheRewardedVideo(tag);
				}
			}

			_rewardVideosCached = true;
		}

		public void CacheInterstitial(string tag)
		{
			IMediationAdsWrapperInterface wrapper = Wrapper;
			if (Enabled && wrapper != null)
				wrapper.CacheInterstitial(tag);
		}

		public void CacheRewardedVideo(string tag)
		{
			IMediationAdsWrapperInterface wrapper = Wrapper;
			if (Enabled && wrapper != null)
				wrapper.CacheRewardedVideo(tag);
		}
		#endregion

		#region Controls
		public void ShowBanner(string tag)
		{
			IMediationAdsWrapperInterface wrapper = Wrapper;
			if (Enabled && wrapper != null)
				wrapper.ShowBanner(tag);
		}

		public void HideBanner()
		{
			IMediationAdsWrapperInterface wrapper = Wrapper;
			if (wrapper != null)
				wrapper.HideBanner();
		}

		public void ShowBetweenLevelAd(string tag, Action callback)
		{
			IMediationAdsWrapperInterface wrapper = Wrapper;
			bool adStarted = false;

			if (Enabled && wrapper != null)
			{
				APIManager.Instance.State.IncreaseInterstitialCounter();

				if (CanDisplayBetweenLevelAd)
				{
					adStarted = true;
					_interstitialClosedCallback = callback;
					Debug.Log("Display interstitial: " + tag);
					APIManager.Instance.State.ResetInterstitialCounter();
					EventManager.SendEvent(IDs.Events.DisableMusicDuringAd);
					#if UNITY_EDITOR
					DelayedCall.Create(OnInterstitialClosed, 2f);
					#else
					wrapper.ShowIntersitital(tag);
					#endif
				}
			}

			if (!adStarted && callback != null)
			{
				callback();
			}

			callback = null;
		}

		public void ShowIntersitital(string tag)
		{
			IMediationAdsWrapperInterface wrapper = Wrapper;

			if (Enabled && wrapper != null)
			{
				EventManager.SendEvent(IDs.Events.DisableMusicDuringAd);
				#if UNITY_EDITOR
				DelayedCall.Create(OnInterstitialClosed, 2f);
				#else
				wrapper.ShowIntersitital(tag);
				#endif
			}
		}

		public void ShowRewardedVideo(string tag)
		{
			IMediationAdsWrapperInterface wrapper = Wrapper;
			if (wrapper != null)
			{
				EventManager.SendEvent(IDs.Events.DisableMusicDuringAd);
				#if UNITY_EDITOR
				DelayedCall.Create(OnInterstitialClosed, 2f);
				#else
				wrapper.ShowRewardedVideo(tag);
				#endif
			}
		}

		private int _testUIClicks = 0;
		private float _testUILastClickTime = 0f;

		public void ShowTestUI(bool useClickCounter)
		{
			if (useClickCounter)
			{
				if (Time.realtimeSinceStartup - _testUILastClickTime <= 0.5f)
				{
					_testUIClicks++;
				}
				else
				{
					_testUIClicks = 0;
				}

				_testUILastClickTime = Time.realtimeSinceStartup;

				if (_testUIClicks >= 5)
				{
					_testUIClicks = 0;
					ShowTestUI(false);
				}
			}
			else
			{
				IMediationAdsWrapperInterface wrapper = Wrapper;
				if (wrapper != null)
				{
					wrapper.ShowTestUI();
				}
			}
		}

		private void DisableAds()
		{
			HideBanner();
		}
		#endregion

		#region Queries
		public float GetBannerHeight()
		{
			IMediationAdsWrapperInterface wrapper = Wrapper;
			if (Enabled && wrapper != null)
				return wrapper.GetBannerHeight();
			else
				return 0f;
		}

		public bool IsBannerVisible()
		{
			IMediationAdsWrapperInterface wrapper = Wrapper;
			if (Enabled && wrapper != null)
				return wrapper.IsBannerVisible();
			else
				return false;
		}

		private bool CanDisplayBetweenLevelAd
		{
			get
			{
				if (Enabled)
					return APIManager.Instance.State.IntertitialCounter >= APIManager.Instance.Settings.IntertitialsDelay;
				else
					return false;
			}
		}
		#endregion

		#region Event Listeners
		protected override void CreateEventListeners() 
		{
			base.CreateEventListeners();

			EventManager.AddListener(IDs.Events.DisableAds, OnDisableAds);

			foreach (IMediationAdsWrapperInterface wrapper in Wrappers)
			{
				wrapper.InterstitialClosed += OnInterstitialClosed;
				wrapper.GrantReward += OnGrantReward;
				wrapper.BannerDisplayed += OnBannerDisplayed;
				wrapper.BannerRemoved += OnBannerRemoved;
			}
		}

		protected override void RemoveEventListeners() 
		{
			base.RemoveEventListeners();

			EventManager.RemoveListener(IDs.Events.DisableAds, OnDisableAds);

			foreach (IMediationAdsWrapperInterface wrapper in Wrappers)
			{
				wrapper.InterstitialClosed -= OnInterstitialClosed;
				wrapper.GrantReward -= OnGrantReward;
				wrapper.BannerDisplayed -= OnBannerDisplayed;
				wrapper.BannerRemoved -= OnBannerRemoved;
			}
		}
		#endregion

		#region Event Handlers
		private void OnDisableAds()
		{
			DisableAds();
		}

		private void OnInterstitialClosed()
		{
			EventManager.SendEvent(IDs.Events.EnableMusicAfterAd);
			if (InterstitialClosed != null)
				InterstitialClosed();

			if (_interstitialClosedCallback != null)
			{
				_interstitialClosedCallback();
				_interstitialClosedCallback = null;
			}
		}

		private void OnGrantReward()
		{
			EventManager.SendEvent(IDs.Events.EnableMusicAfterAd);
			if (GrantReward != null)
				GrantReward();
		}

		private void OnBannerDisplayed()
		{
			if (BannerDisplayed != null)
				BannerDisplayed();
		}

		private void OnBannerRemoved()
		{
			if (BannerRemoved != null)
				BannerRemoved();
		}
		#endregion

		#region Getters
		protected IMediationAdsWrapperInterface Wrapper
		{
			get { return (IMediationAdsWrapperInterface)CurrentWrapperBase; }
		}
		#endregion

		#region Helpers
		private bool Enabled
		{
			get 
			{
				return APIManager.Instance.State.AdsEnabled && APIManager.Instance.Settings.AdsEnabled;
			}
		}
		#endregion
	}
}

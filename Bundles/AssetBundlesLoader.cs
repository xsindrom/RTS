using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Network.GameServer;
using Utilities;
public class AssetBundlesLoader : MonoSingleton<AssetBundlesLoader>
{
    [Serializable]
    public class BundlesContainer
    {
        public List<BundleListResponse.SBundle> bundles = new List<BundleListResponse.SBundle>();
    }

    private const string POST = "POST";

    private const string BUNDLES_PATH = "bundles";
    private const string BUNDLES_MANIFEST_PATH = "bundles_manifest";

    private const string CONTENT_TYPE_KEY = "Content-type";
    private const string CONTENT_TYPE_VALUE_APPLICATION_WWW = "application/x-www-form-urlencoded";
    private const string CONTENT_TYPE_VALUE_APPLICATION_JSON = "application/json";
    private const string CONTENT_TYPE_VALUE_TEXT = "text/plain";

    private BundlesContainer localBundles = new BundlesContainer();
    private Dictionary<string, AssetBundle> assetBundles = new Dictionary<string, AssetBundle>();
    private Dictionary<string, UnityEngine.Object> cachedAssets = new Dictionary<string, UnityEngine.Object>();
    private Dictionary<string, float> bundlesLoading = new Dictionary<string, float>();

    private static List<CustomEventListener<string[]>> bundlesListeners = new List<CustomEventListener<string[]>>();
    public static event Action<float> OnBundlesLoading;
    public static event Action<string[]> OnBundlesLoaded;
    
    protected override void Init()
    {
        base.Init();
        SceneLoader.Instance.OnSceneLoaded += OnSceneLoaded;
        OnBundlesLoading += OnProcessBundlesLoading;
    }

    public static void SubscribeOrGetOnBundlesLoaded(object eventHandler, Action<string[]> callback, params string[] bundles)
    {
        if(Instance && Array.TrueForAll(bundles, x=> Instance.assetBundles.ContainsKey(x)))
        {
            if (eventHandler != null)
            {
                EventHelper.SafeCall(callback, bundles);
            }
        }
        else
        {
            var eventListener = new CustomEventListener<string[]>(eventHandler, bundles);
            eventListener.SetSubscribe(callback);
            eventListener.SetUnSubscribe(() => OnBundlesLoaded -= eventListener.Invoke);
            eventListener.SetPredicate((data) => Array.TrueForAll(eventListener.Data, x => data.Contains(x)));
            bundlesListeners.Add(eventListener);
            OnBundlesLoaded += eventListener.Invoke;
        }
    }

    public static void UnSubscribeOnBundlesLoaded(object eventHandler)
    {
        var toRemove = bundlesListeners.FindAll(x => x.EventHandler == eventHandler);
        for(int i = 0; i < toRemove.Count; i++)
        {
            var listener = toRemove[i];
            OnBundlesLoaded -= listener.Invoke;
        }
        bundlesListeners.RemoveAll(x => x.EventHandler == eventHandler);
    }

    protected void OnProcessBundlesLoading(float progress)
    {
        if(progress == 1)
        {
            SaveBundleList(localBundles.bundles);
            OnBundlesLoading -= OnProcessBundlesLoading;
        }
    }

    protected void OnSceneLoaded(string prevScene, string newScene)
    {
        if(newScene == SceneConstants.MAIN_SCENE)
        {
            LoadBundleList();
            CheckBundleList();
            SceneLoader.Instance.OnSceneLoaded -= OnSceneLoaded;
        }
    }

    public void LoadBundleList()
    {
        var path = $"{Application.persistentDataPath}/bundles.json";
        if (!File.Exists(path))
            return;

        var json = File.ReadAllText(path);
        localBundles = JsonUtility.FromJson<BundlesContainer>(json);
    }

    public void SaveBundleList(List<BundleListResponse.SBundle> bundles)
    {
        BundlesContainer bundlesContainer = new BundlesContainer()
        {
            bundles = bundles
        };
        var json = JsonUtility.ToJson(bundlesContainer, true);
        File.WriteAllText($"{Application.persistentDataPath}/bundles.json", json);
    }

    public void CheckBundleList()
    {
        var settings = ResourceManager.GetGeneralSettings();
        var request = new BundleListRequest()
        {
            path = GameServerConstants.BUNDLE_LIST,
            platform = settings.CurrentPlatform,
            bundleVersion = settings.BundleVersion
        };
        GameServer.Instance.Api.Post<BundleListRequest, BundleListResponse>(request, OnBundleList);
    }

    private float GetBundleProgress()
    {
        var sum = bundlesLoading.Sum(x => x.Value);
        return bundlesLoading.Count == 0 ? 0 : sum / bundlesLoading.Count;
    }

    private void OnBundleList(BundleListResponse response)
    {
        var settings = ResourceManager.GetGeneralSettings();
        if (response.status == Status.OK)
        {
            var bundlesToLoad = new List<string>();
            for (int i = 0; i < response.bundleList.Length; i++)
            {
                var bundle = response.bundleList[i];

                var localIndex = localBundles.bundles.FindIndex(x => x.bundleName == bundle.bundleName);
                if (localIndex == -1)
                {
                    localBundles.bundles.Add(bundle);
                    bundlesToLoad.Add(bundle.bundleName);
                }
                else
                {
                    if (bundle.crc != localBundles.bundles[localIndex].crc)
                    {
                        var newLocalBundle = localBundles.bundles[localIndex];
                        newLocalBundle.crc = bundle.crc;
                        localBundles.bundles[localIndex] = newLocalBundle;

                        bundlesToLoad.Add(bundle.bundleName);
                    }
                }
            }

            for (int i = 0; i < bundlesToLoad.Count; i++)
            {
                var bundleToLoad = bundlesToLoad[i];
                bundlesLoading.Add(bundleToLoad, 0);
                LoadAssetBundle(bundleToLoad, settings.CurrentPlatform, settings.BundleVersion);
            }
        }

        for (int i = 0; i < localBundles.bundles.Count; i++)
        {
            var bundle = localBundles.bundles[i];
            if (!bundlesLoading.ContainsKey(bundle.bundleName))
            {
                var hash = Hash128.Parse(bundle.hash);
                if (hash.isValid)
                {
                    bundlesLoading.Add(bundle.bundleName, 0);
                    LoadAssetBundleFromCache(bundle.bundleName, settings.CurrentPlatform, settings.BundleVersion, hash);
                }
            }
        }
    }
  
    public void LoadAssetBundle(string bundleName, string platform, int bundleVersion)
    {
        StartCoroutine(LoadAssetByManifest(bundleName, platform, bundleVersion));
    }

    private Hash128 GetHash128FromManifest(string manifest)
    {
        Hash128 hash = default;
        var hashRow = manifest.ToString().Split("\n".ToCharArray())[5];
        hash = Hash128.Parse(hashRow.Split(':')[1].Trim());
        return hash;
    }

    private IEnumerator LoadAssetByManifest(string bundleName, string platform, int bundleVersion)
    {
        while (!Caching.ready)
            yield return null;

        var settings = ResourceManager.GetGeneralSettings();
        var manifestPath = $"{settings.ServerUrl}{BUNDLES_MANIFEST_PATH}?platform={platform}&bundleName={bundleName}&bundleVersion={bundleVersion}";
        using (var manifestRequest = UnityWebRequest.Get(manifestPath))
        {
            manifestRequest.SetRequestHeader(CONTENT_TYPE_KEY, CONTENT_TYPE_VALUE_TEXT);
            yield return manifestRequest.SendWebRequest();

            if (manifestRequest.isNetworkError || manifestRequest.isHttpError)
            {
                Debug.Log("Error loading bundle");
            }

            Hash128 hash = GetHash128FromManifest(manifestRequest.downloadHandler.text);
            if (hash.isValid)
            {
                yield return LoadAssetBundleRoutine(bundleName, platform, bundleVersion, hash);
            }
        }
    }

    public void LoadAssetBundleFromCache(string bundleName, string platform, int bundleVersion, Hash128 hash)
    {
        StartCoroutine(LoadAssetBundleRoutine(bundleName, platform, bundleVersion,  hash));
    }

    private IEnumerator LoadAssetBundleRoutine(string bundleName, string platform, int bundleVersion, Hash128 hash)
    {
        var settings = ResourceManager.GetGeneralSettings();
#if UNITY_EDITOR
        var cachePath = $"{Application.persistentDataPath}_EditorCache";
#else
            var cachePath = $"{Application.persistentDataPath}_AppCache";                               
#endif

        if (!Directory.Exists(cachePath))
            Directory.CreateDirectory(cachePath);

        if (Caching.currentCacheForWriting.path != cachePath)
            Caching.currentCacheForWriting = Caching.AddCache(cachePath);

        var path = $"{settings.ServerUrl}{BUNDLES_PATH}?platform={platform}&bundleName={bundleName}&bundleVersion={bundleVersion}";
        using (var www = UnityWebRequestAssetBundle.GetAssetBundle(path, hash, 0))
        {
            www.SendWebRequest();
            while (!www.isDone)
            {
                yield return null;

                var progress = www.downloadProgress;
                bundlesLoading[bundleName] = progress;

                var localIndex = localBundles.bundles.FindIndex(x => x.bundleName == bundleName);
                if (localIndex != -1)
                {
                    var newLocalBundle = localBundles.bundles[localIndex];
                    newLocalBundle.hash = hash.ToString();
                    localBundles.bundles[localIndex] = newLocalBundle;
                }

                EventHelper.SafeCall(OnBundlesLoading, GetBundleProgress());
            }

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log($"Error loading bundle {bundleName}");
            }
            var bundle = DownloadHandlerAssetBundle.GetContent(www);
            if (!bundle)
                yield break;

            List<Hash128> listOfCachedVersions = new List<Hash128>();
            Caching.GetCachedVersions(bundle.name, listOfCachedVersions);

            for (int i = 0; i < listOfCachedVersions.Count; i++)
            {
                Debug.Log(listOfCachedVersions[i].ToString());
            }
            assetBundles[bundle.name] = bundle;
            EventHelper.SafeCall(OnBundlesLoaded, assetBundles.Keys.ToArray());
        }
    }

    public T GetAsset<T>(string bundleName, string assetName, string assetPath = null) where T : UnityEngine.Object
    {
        var key = string.Empty;
        key = !string.IsNullOrEmpty(assetPath) ? string.Format("{0}_{1}_{2}", bundleName, assetPath, assetName) :
                                                 string.Format("{0}_{1}", bundleName, assetName);

        if (cachedAssets.ContainsKey(key))
        {
            return cachedAssets[key] as T;
        }
        else
        {
            if (assetBundles.ContainsKey(bundleName))
            {
                var asset = assetBundles[bundleName];
                if (asset)
                {
                    var loaded = asset.LoadAsset<T>(assetName);
                    if (loaded)
                    {
                        cachedAssets.Add(key, loaded);
                        return loaded;
                    }
                }
                else
                {
                    Debug.Log("No asset with name  " + assetName + " in bundle " + bundleName + " found");
                }
            }
            else
            {
                Debug.Log("No bundles with name: " + bundleName + " found");
            }
        }
        return null;
    }

    public override void Restart()
    {
        foreach(var assetBundle in assetBundles)
        {
            assetBundle.Value.Unload(true);
        }
        base.Restart();
    }
}

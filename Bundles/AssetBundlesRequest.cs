using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Network.GameServer
{
    [Serializable]
    public class AssetBundlesRequestData
    {
        public string bundleName;
        public string platform;
        public int bundleVersion;
    }

    [Serializable]
    public class AssetBundlesRequest : GameServerBaseRequest
    {
        public AssetBundlesRequestData data;
    }
}
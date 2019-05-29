using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Network.GameServer;

[Serializable]
public class BundleListResponse : GameServerBaseResponse
{
    [Serializable]
    public struct SBundle
    {
        public string bundleName;
        public uint crc;
        public string hash;
    }

    public SBundle[] bundleList;
}

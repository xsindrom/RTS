using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Network.GameServer;

[Serializable]
public class BundleListRequest : GameServerBaseRequest
{
    public string platform;
    public int bundleVersion;
}

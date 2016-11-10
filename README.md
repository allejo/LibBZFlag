# LibBZFlag
A library implementaiton of bzflag features

## BZFlag.Authentication
A basic list server client with authentication

##BZFlag.Data
Common shared data structures

##BZFlag.IO.BZW
Library for reading/writing and storing bzworld objects


## BZFlag.Networking.Client
Network connection class for bzflag clients

### Messages 
/// Main system messages
* **DONE** MsgAccept = 0x6163;			    // 'ac'          
* **DONE** MsgAdminInfo = 0x6169;			// 'ai'
* **DONE** MsgAlive = 0x616c;			    // 'al'
* **DONE** MsgAddPlayer = 0x6170;			// 'ap'
* **DONE** MsgAutoPilot = 0x6175;			// 'au'
* **DONE** MsgCaptureFlag = 0x6366;		// 'cf'
* **DONE** MsgCustomSound = 0x6373;		// 'cs'
* **DONE** MsgCacheURL = 0x6375;			  // 'cu'
* **DONE** MsgDropFlag = 0x6466;			  // 'df'
* **DONE** MsgEnter = 0x656e;			    // 'en'
* **DONE** MsgExit = 0x6578;			      // 'ex'
* **DONE** MsgFlagType = 0x6674;			    // 'ft'
* **DONE** MsgFlagUpdate = 0x6675;			// 'fu'
* **DONE** MsgFetchResources = 0x6672;		// 'fr'
* **DONE** MsgGrabFlag = 0x6766;			// 'gf'
* **DONE** MsgGMUpdate = 0x676d;			// 'gm'
* **DONE** MsgGetWorld = 0x6777;			// 'gw'
* **DONE** MsgGameSettings = 0x6773;		// 'gs'
* **DONE** MsgGameTime = 0x6774;			// 'gt'
* **DONE** MsgHandicap = 0x6863;		   // 'hc'
* **DONE** MsgKilled = 0x6b6c;			// 'kl'
* **UNUSED** MsgLagState = 0x6c73;			// 'ls'
* **DONE** MsgMessage = 0x6d67;			// 'mg'
* **DONE** MsgNearFlag = 0x4e66;		   // 'Nf'
* **DONE** MsgNewRabbit = 0x6e52;			// 'nR'
* **DONE??** MsgNegotiateFlags = 0x6e66;		// 'nf'
* **DONE** MsgPause = 0x7061;			// 'pa'
* **DONE** MsgPlayerInfo = 0x7062;			// 'pb'
* **DONE** MsgPlayerUpdate = 0x7075;		// 'pu'
* **DONE** MsgPlayerUpdateSmall = 0x7073;		// 'ps'
* **DONE** MsgQueryGame = 0x7167;			// 'qg'
* **DONE** MsgQueryPlayers = 0x7170;		// 'qp'
* **DONE** MsgReject = 0x726a;			// 'rj'
* **DONE** MsgRemovePlayer = 0x7270;		// 'rp'
* **UNSUPPORTED** MsgReplayReset = 0x7272;		// 'rr'
* **DONE** MsgShotBegin = 0x7362;			// 'sb'
* **DONE** MsgScore = 0x7363;			// 'sc'
* **DONE** MsgScoreOver = 0x736f;			// 'so'
* **DONE** MsgShotEnd = 0x7365;			// 'se'
* **DONE** MsgSuperKill = 0x736b;			// 'sk'
* **DONE** MsgSetVar = 0x7376;			// 'sv'
* **DONE** MsgTimeUpdate = 0x746f;			// 'to'
* **DONE** MsgTeleport = 0x7470;			// 'tp'
* **DONE** MsgTransferFlag = 0x7466;		// 'tf'
* **DONE** MsgTeamUpdate = 0x7475;			// 'tu'
* **DONE** MsgWantWHash = 0x7768;			// 'wh'
* **DONE** MsgWantSettings = 0x7773;		// 'ws'
* **UNUSED** MsgPortalAdd = 0x5061;			// 'Pa'
* **UNUSED** MsgPortalRemove = 0x5072;		// 'Pr'
* **UNUSED** MsgPortalUpdate = 0x5075;		// 'Pu'

// world database codes
* WorldCodeHeader = 0x6865;		// 'he'
* WorldCodeBase = 0x6261;			// 'ba'
* WorldCodeBox = 0x6278;			// 'bx'
* WorldCodeEnd = 0x6564;			// 'ed'
* WorldCodeLink = 0x6c6e;			// 'ln'
* WorldCodePyramid = 0x7079;		// 'py'
* WorldCodeMesh = 0x6D65;			// 'me'
* WorldCodeArc = 0x6172;			// 'ar'
* WorldCodeCone = 0x636e;			// 'cn'
* WorldCodeSphere = 0x7370;		// 'sp'
* WorldCodeTetra = 0x7468;		// 'th'
* WorldCodeTeleporter = 0x7465;		// 'te'
* WorldCodeWall = 0x776c;			// 'wl'
* WorldCodeWeapon = 0x7765;		// 'we'
* WorldCodeZone = 0x7A6e;			// 'zn'
* WorldCodeGroup = 0x6772;		// 'gr'
* WorldCodeGroupDefStart = 0x6473;	// 'ds'
* WorldCodeGroupDefEnd = 0x6465;		// 'de'

// world database sizes
* WorldSettingsSize = 30;
* WorldCodeHeaderSize = 10;
* WorldCodeBaseSize = 31;
* WorldCodeWallSize = 24;
* WorldCodeBoxSize = 29;
* WorldCodeEndSize = 0;
* WorldCodePyramidSize = 29;
* WorldCodeMeshSize = 0xA5;  // dummy value, sizes are variable
* WorldCodeArcSize = 85;
* WorldCodeConeSize = 65;
* WorldCodeSphereSize = 53;
* WorldCodeTetraSize = 66;
* WorldCodeTeleporterSize = 34;
* WorldCodeLinkSize = 4;
* WorldCodeWeaponSize = 24;  // basic size, not including lists
* WorldCodeZoneSize = 34;    // basic size, not including lists

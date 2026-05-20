//==============================================================================

function getURLParameter(url, name, defaultValue) {
	if (!url)
		return defaultValue;

	if (url.indexOf("?") == -1)
		return defaultValue;

	var param = defaultValue;
	var paramsString = url.substring(url.indexOf("?") + 1, url.length);
	var params = paramsString.split("&");
	for (var i = 0; i < params.length; i++) {
		if (params[i].indexOf(name + '=') != 0)
			continue;

		param = params[i].substring(
			(name + '=').length, params[i].length);
		break;
	}

	return param;
}

var EU_LOG_EVENTS = getURLParameter(location.href, 'log', "false") == "true" ? 
	true : false;
var EU_MAX_DATA_SIZE_MB = parseInt(
	getURLParameter(location.href, 'maxFileSize', '5'));
var EU_VERSION = getURLParameter(location.href, 'v', (new Date).getTime());

//==============================================================================

importScripts(
	'euscpt.js?v=' + EU_VERSION,
	'euscpm.js?v=' + EU_VERSION,
	'euscp.ex.js?v=' + EU_VERSION,
	'../lib/jszip.min.js?v=' + EU_VERSION);

//==============================================================================

var ASIC_S_FILE_NAME_MIMETYPE = "mimetype";
var ASIC_S_FILE_NAME_SIGNATURE = "signature.p7s";
var ASIC_S_FOLDER_NAME_META_INF = "META-INF";
var ASIC_S_MIMETYPE = "application/vnd.etsi.asic-s+zip";
var ASIC_S_FILES_EXTENSIONS = ['zip', 'asics', 'scs', 'asice', 'sce', 'odt'];

//-----------------------------------------------------------------------------

var EUSignContainerType = {
	Unknown:			0x0000,
	Detached:			0x0001,
	Enveloped:			0x0002,
	Enveloping:			0x0004,
	Multi: 				0x0008,
	Base64: 			0x0010,
	ASiCS: 				0x0020,
	ASiCE:				0x0040,
	CAdES:				0x0080,
	XAdES:				0x0100,
	PAdES:				0x0200
};

//-----------------------------------------------------------------------------

var EUVerifyErrorCode = {
	NoError: 			0,
	NoFileWithData: 	1,
	InvalidFileFormat:	2,
	FileTooBig: 		3
};

//-----------------------------------------------------------------------------

var EUVerifySignInfo = function(
	signerInfo, signTimeInfo, 
	isDigitalStamp, qscd, signAlgoType, signAlgo,
	signFormatType, signFormat, signContainerType, 
	signContainer, isAllContentCovered, signReferences) {
	this.signerInfo = signerInfo;
	this.signTimeInfo = signTimeInfo;
	this.isDigitalStamp = isDigitalStamp;
	this.qscd = qscd;
	this.signAlgoType = signAlgoType;
	this.signAlgo = signAlgo;
	this.signFormatType = signFormatType;
	this.signFormat = signFormat;
	this.signContainerType = signContainerType;
	this.signContainer = signContainer;
	this.isAllContentCovered = isAllContentCovered;
	this.signReferences = signReferences;
};

//-----------------------------------------------------------------------------

var EUVerifyResult = function(
	resultCode, signFile, dataFile, 
	signType, signsInfos, data, datas) {
	this.resultCode = resultCode;
	this.signFile = signFile;
	this.dataFile = dataFile;
	this.signType = signType;
	this.signsInfos = signsInfos;
	this.data = data;
	this.datas = datas;
};

//==============================================================================

var s_settings = null;

var s_loaded = false;

var s_verifyLargeFiles = true;
var s_verifyMultiSign = true;
var s_verifyBase64Sign = true;

var s_origin = '';
var s_pathname = '';
var s_signInfoTmpl = null;

var s_maxFileSize = EU_MAX_DATA_SIZE_MB * EU_ONE_MB;

//==============================================================================

function sendMessage(data, e, params) {
	var error = (e != null) ? 
		{'errorCode': e.errorCode, 'message': e.message} : null;
	self.postMessage({
			'cmd': data.cmd,
			'callback_id': data.callback_id,
			'error': error,
			'params': params
		});
}

//==============================================================================

function MakeURL(url) {
	if (url.indexOf('http://') == 0 ||
		url.indexOf('https://') == 0) {
		return url;
	}

	if ((url.indexOf('/') == 0) && s_origin) {
		return s_origin + url;
	}

	if (s_origin && s_pathname) {
		return s_origin + 
			s_pathname.substr(0, s_pathname.lastIndexOf('/')) + 
			'/' + url;
	}

	return url;
}

//-------------------------------------------------------------------------------

function DowloadData(url, dataType) {
	try {
		var xmlHttp = XMLHttpRequest ? 
			(new XMLHttpRequest()) : 
			new ActiveXObject("Microsoft.XMLHTTP");

		url = MakeURL(url);

		xmlHttp.open("GET", url, false);
		if (dataType == 'binary')
			xmlHttp.responseType = 'arraybuffer';
		xmlHttp.send();
		if (xmlHttp.status != 200) {
			throw 'Download data error. URL - ' + 
				url + ', status - ' + xmlHttp.status;
		}

		switch (dataType) {
			case 'binary':
				return new Uint8Array(xmlHttp.response);

			case 'json':
				return JSON.parse(xmlHttp.responseText.replace(/\\'/g, "'"));

			default:
				return xmlHttp.responseText;
		}
	} catch (e) {
		console.log("EndUser.DowloadData error: " + e);
		euSign.RaiseError(EU_ERROR_DOWNLOAD_FILE);
	}
}

//------------------------------------------------------------------------------

function SetXMLHTTPDirectAccess(cas) {
	try {
		var _addDNSName = function(uri, dnsNames) {
			if (uri == '')
				return;

			uri = (uri.indexOf("://") > -1) ? 
				uri.split('/')[2] : 
				uri.split('/')[0];

			if (dnsNames.indexOf(uri) >= 0)
				return;

			dnsNames.push(uri);
		}; 

		euSign.SetXMLHTTPDirectAccess(true);
		var directAccessURLs = Array();
		_addDNSName('czo.gov.ua', directAccessURLs);
		for (var i = 0; i < cas.length; i++) {
			if (!cas[i].directAccess)
				continue;

			_addDNSName(cas[i].address, directAccessURLs);
			_addDNSName(cas[i].tspAddress, directAccessURLs);
			_addDNSName(cas[i].cmpAddress, directAccessURLs);
			_addDNSName(cas[i].ocspAccessPointAddress,
				directAccessURLs);
		}

		directAccessURLs.forEach(function(address) {
			euSign.AddXMLHTTPDirectAccessAddress(address);
		});
	} catch (e) {
	}
}

//------------------------------------------------------------------------------

function SetSettings(cas, httpProxyServiceURL, directAccess, tslAddress) {
	euSign.SetXMLHTTPProxyService(MakeURL(httpProxyServiceURL));
	if (directAccess)
		SetXMLHTTPDirectAccess(cas);

	euSign.SetRuntimeParameter(
		EU_CHECK_CERT_CHAIN_ON_SIGN_TIME_PARAMETER, true);

	var settings = euSign.CreateFileStoreSettings();
	settings.SetPath('');
	settings.SetCheckCRLs(true);
	settings.SetOwnCRLsOnly(false);
	euSign.SetFileStoreSettings(settings);

	settings = euSign.CreateProxySettings();
	euSign.SetProxySettings(settings);

	settings = euSign.CreateTSPSettings();
	euSign.SetTSPSettings(settings);

	settings = euSign.CreateOCSPSettings();
	settings.SetUseOCSP(true);
	settings.SetBeforeStore(true);
	settings.SetAddress('');
	settings.SetPort('80');
	euSign.SetOCSPSettings(settings);

	settings = euSign.CreateOCSPAccessInfoModeSettings();
	settings.SetEnabled(true);
	euSign.SetOCSPAccessInfoModeSettings(settings);

	settings = euSign.CreateOCSPAccessInfoSettings();
	for (var i = 0; i < cas.length; i++) {
		settings.SetAddress(cas[i].ocspAccessPointAddress);
		settings.SetPort(cas[i].ocspAccessPointPort);

		for (var j = 0; j < cas[i].issuerCNs.length; j++) {
			settings.SetIssuerCN(cas[i].issuerCNs[j]);
			euSign.SetOCSPAccessInfoSettings(settings);
		}
	}

	settings = euSign.CreateCMPSettings();
	euSign.SetCMPSettings(settings);

	settings = euSign.CreateLDAPSettings();
	euSign.SetLDAPSettings(settings);

	var useTSL = !!tslAddress;
	settings = euSign.CreateTSLSettings();
	settings.SetUseTSL(useTSL);
	if (useTSL) {
		settings.SetAutoDownloadTSL(useTSL);
		settings.SetTSLAddress(tslAddress);
	}
	euSign.SetTSLSettings(settings);
}

//------------------------------------------------------------------------------

function IsSignerDigitalTimeStamp(info) {
	return info.GetExtKeyUsages().indexOf(UA_OID_EXT_KEY_USAGE_STAMP) > -1 ||
		info.GetExtKeyUsages().indexOf("Цифрова печатка") > -1;
}

//------------------------------------------------------------------------------

function GetSignAlgoType(info) {
	switch (info.GetPublicKeyType()) {
		case EU_CERT_KEY_TYPE_DSTU4145:
			return info.GetCertHashType() == EU_CERT_HASH_TYPE_GOST34311 ? 
				EU_CTX_SIGN_DSTU4145_WITH_GOST34311 : 
				EU_CTX_SIGN_DSTU4145_WITH_DSTU7564;
		case EU_CERT_KEY_TYPE_RSA:
			return EU_CTX_SIGN_RSA_WITH_SHA;
		case EU_CERT_KEY_TYPE_ECDSA:
				return EU_CTX_SIGN_ECDSA_WITH_SHA;
		default:
			return EU_CTX_SIGN_UNKNOWN;
	}
}
//------------------------------------------------------------------------------

function GetSignAlgo(info) {
	switch (info.GetPublicKeyType()) {
		case EU_CERT_KEY_TYPE_DSTU4145:
			return "ДСТУ 4145";
		case EU_CERT_KEY_TYPE_RSA:
			return "RSA";
		case EU_CERT_KEY_TYPE_ECDSA:
				return "ECDSA";	
		default:
			return '';
	}
}

//------------------------------------------------------------------------------

function GetSignContainerType(signContainerType) {
	if (signContainerType & EUSignContainerType.ASiCS) {
		return 'Підпис та дані в архіві (ASiC-S)';
	} else if (signContainerType & EUSignContainerType.ASiCE) {
		return 'Підпис та дані в архіві (розширений) (ASiC-E)';
	} else if (signContainerType & EUSignContainerType.PAdES) {
		return 'Підписаний PDF-файл (PAdES)';
	} else if (signContainerType & EUSignContainerType.CAdES) {
		if (signContainerType & EUSignContainerType.Enveloped)
			return 'Підпис та дані в одному файлі (CAdES enveloped)';
		else if (signContainerType & EUSignContainerType.Detached)
			return 'Підпис та дані в окремих файлах (CAdES detached)';
		else 
			return 'Не визначено';
	} else if (signContainerType & EUSignContainerType.XAdES) {
		if (signContainerType & EUSignContainerType.Enveloped)
			return 'Підпис та дані в одному файлі (XAdES enveloped)';
		else if (signContainerType & EUSignContainerType.Detached)
			return 'Підпис та дані в окремих файлах (XAdES detached)';
		else if (signContainerType & EUSignContainerType.Enveloping)
			return 'Підпис та дані в одному файлі (XAdES enveloping)';
		else 
			return 'Не визначено';
	}
}

//------------------------------------------------------------------------------

function GetCAdESSignLevel(signLevel) {
	switch (signLevel) {
		case EU_SIGN_TYPE_CADES_BES:
			return "Базовий (CAdES-BES)";
		case EU_SIGN_TYPE_CADES_T:
			return "З позначкою часу від ЕП (CAdES-T)";
		case EU_SIGN_TYPE_CADES_C:
			return "З посиланнями на повні дані для перевірки (CAdES-C)";
		case EU_SIGN_TYPE_CADES_X_LONG:
			return "З повними даними для перевірки (CAdES-X Long)";
		case EU_SIGN_TYPE_CADES_X_LONG | 
			EU_SIGN_TYPE_CADES_X_LONG_TRUSTED:
			return "З повними даними ЦСК для перевірки (CAdES-X Long)";	
		default:
			return 'CAdES (Не визначено)';
	}
}

//------------------------------------------------------------------------------

function GetXAdESSignLevel(signLevel) {
	switch (signLevel) {
		case EU_XADES_SIGN_LEVEL_B_B:
			return "Базовий (XAdES-B-B)";
		case EU_XADES_SIGN_LEVEL_B_T:
			return "З позначкою часу від ЕП (XAdES-B-T)";
		case EU_XADES_SIGN_LEVEL_B_LT:
			return "З повними даними для перевірки (XAdES-B-LT)";
		case EU_XADES_SIGN_LEVEL_B_LTA:
			return "З повними даними для архівного зберігання (XAdES-B-LTA)";
		default:
			return 'XAdES (Не визначено)';
	}
}

//------------------------------------------------------------------------------

function GetPAdESSignLevel(signLevel) {
	switch (signLevel) {
		case EU_PADES_SIGN_LEVEL_B_B:
			return "Базовий (PAdES-B-B)";
		case EU_PADES_SIGN_LEVEL_B_T:
			return "З позначкою часу від ЕП (PAdES-B-T)";
		case EU_PADES_SIGN_LEVEL_B_LT:
			return "З повними даними для перевірки (PAdES-B-LT)";
		case EU_PADES_SIGN_LEVEL_B_LTA:
			return "З повними даними для архівного зберігання (PAdES-B-LTA)";
		default:
			return 'PAdES (Не визначено)';
	}
}	

//------------------------------------------------------------------------------

function GetHashAlgoForCertificate(info) {
	return (info.GetPublicKeyType() == EU_CERT_KEY_TYPE_DSTU4145) ?
		(info.GetCertHashType() == EU_CERT_HASH_TYPE_GOST34311 ? 
			EU_CTX_HASH_ALGO_GOST34311 : EU_CTX_HASH_ALGO_DSTU7564_256): 
		EU_CTX_HASH_ALGO_SHA256;
}

//------------------------------------------------------------------------------

function IsUseCRLs(issuerCN) {
	if (!s_settings.CRLs)
		return false;

	var CRLs = s_settings.CRLs;

	for (var i = 0; i < CRLs.length; i++) {
		if (CRLs[i].issuerCNs.indexOf(issuerCN) > -1) {
			if (!CRLs[i].downloaded) {
				for (var j = 0; j < CRLs[i].CRLs.length; j++) {
					var crl = DowloadData(CRLs[i].CRLs[j], 'binary');
					euSign.SaveCRL(true, crl);
				}
				
				CRLs[i].downloaded = true;
			}

			return true;
		}
	}

	return false;
}

//------------------------------------------------------------------------------

function IsQSCDSNInCert(issuerCN) {
	for (var i = 0; i < s_settings.CAs.length; i++) {
		var CA = s_settings.CAs[i];
		if (CA.qscdSNInCert && 
			CA.issuerCNs.indexOf(issuerCN) > -1) {
			return true;
		}
	}

	return false;
}

//------------------------------------------------------------------------------

function GetQSCD(cert) {
	var qscd = {
		'use': false,
		'name': null,
		'sn': null
	};

	try {
		var infoEx = euSign.ParseCertificateEx(cert);
		qscd.use = infoEx.IsQSCD();

		if (qscd.use) {
			var extKeyUsages = infoEx.GetExtKeyUsages().split(',');
			for (var i = 0; i < extKeyUsages.length; i++) {
				if (extKeyUsages[i].indexOf("ЗНКІ") > -1) {
					qscd.name = extKeyUsages[i].trim();
					break;
				}
			}

			if (qscd.name && IsQSCDSNInCert(infoEx.GetIssuerCN())) {
				qscd.sn = infoEx.GetSubjUserCode();
			}
		} else if ((infoEx.GetPublicKeyType() == 
				EU_CERT_KEY_TYPE_ECDSA ||
			infoEx.GetPublicKeyType() == 
				EU_CERT_KEY_TYPE_RSA) &&
			IsQSCDSNInCert(infoEx.GetIssuerCN()) && 
			infoEx.GetSubjUserID() && 
			(infoEx.GetSubjUserID().length == 20)) {
			qscd.use = true;
			qscd.name = "ЗНКІ SIM-карта Київстар (MobileID)";
			qscd.sn = infoEx.GetSubjUserID().substr(0, 19);
		}

		if (qscd.use) {
			qscd.name = qscd.name || 'Захищений';
			qscd.sn = qscd.sn || 'Не визначено';
		} else {
			qscd.name = 'Незахищений';
			qscd.sn = '';
		}

		return qscd;
	} catch (e) {
		qscd.use = false;
	}

	return qscd;
}

//------------------------------------------------------------------------------

function CreateSignInfoResult(signInfo, signerCert, signFormat, 
	signContainerType, isAllContentCovered, signReferences) {
	var infoEx = signerCert.GetInfoEx().GetTransferableObject();
	infoEx.isPowerCert = infoEx.isPowerCert;
	infoEx.isQSCD = infoEx.isQSCD;
	return new EUVerifySignInfo(
		infoEx,
		signInfo.GetTimeInfo().GetTransferableObject(),
		IsSignerDigitalTimeStamp(signerCert.GetInfoEx()),
		GetQSCD(signerCert.GetData()),
		GetSignAlgoType(signerCert.GetInfoEx()),
		GetSignAlgo(signerCert.GetInfoEx()),
		signFormat,
		(signContainerType & EUSignContainerType.XAdES) ?
			GetXAdESSignLevel(signFormat) : 
			(signContainerType & EUSignContainerType.PAdES) ?
				GetPAdESSignLevel(signFormat) :
				GetCAdESSignLevel(signFormat),
		signContainerType,
		GetSignContainerType(signContainerType),
		isAllContentCovered, signReferences);
}

//==============================================================================

function Initialize(data) {
	try {
		s_origin = data.params.origin;
		s_pathname = data.params.pathname;
		s_settings = data.params.settings;

		if (typeof (s_settings.CAs) == 'string') {
			s_settings.CAs = DowloadData(s_settings.CAs, 'json');
		}
		s_settings.CACertificates = DowloadData(
			s_settings.CACertificates, 'binary');
		s_settings.verifySettings.signInfoTmpl = DowloadData(
			s_settings.verifySettings.signInfoTmpl);
		
		euSign.SetErrorMessageLanguage(s_settings.language);

		euSign.Initialize();
		euSign.SetOCSPResponseExpireTime(
			s_settings.verifySettings.ocspResponseExpireTime);
		euSign.SetRuntimeParameter(
			EU_STRING_ENCODING_PARAMETER, EU_UTF8_ENCODING);

		SetSettings(s_settings.CAs, 
			s_settings.httpProxyServiceURL,
			s_settings.directAccess,
			s_settings.TSLAddress);
		euSign.SaveCertificates(
			s_settings.CACertificates);

		var params = {
			'signInfoTmpl': s_settings.verifySettings.signInfoTmpl
		};

		sendMessage(data, null, params);
	} catch (e) {
		sendMessage(data, e, null);
		return;
	}
}

//==============================================================================

function GetDataFromSignedFile(data) {
	var signedFile = data.params.signedFile;

	var _onSuccess = function(signedFileData) {
		var params = {'data': signedFileData};
		self.sendMessage(data, null, params);
		signedFileData = null;
	};

	var _onError = function(e) {
		sendMessage(data, e, null);
	};

	euSign.GetDataFromSignedFile(signedFile, _onSuccess, _onError);
}

//==============================================================================

function VerifyExternalData(data, sign) {
	var signsInfos = [];

	var context = null;

	try {
		context = euSign.CtxCreate();
		euSign.CtxSetParameter(context, EU_RESOLVE_OIDS_PARAMETER, false);
		
		var signers = euSign.CtxGetSignsCount(context, sign);
		if (signers <= 0)
			euSign.RaiseError(EU_ERROR_PKI_FORMATS_FAILED);

		for (var i = 0; i < signers; i++) {
			var signerCert = euSign.CtxGetSignerInfo(context, i, sign);
			var hashAlgo = GetHashAlgoForCertificate(signerCert.GetInfoEx());
			var hashContext = euSign.CtxHashDataBegin(
				context, hashAlgo, signerCert.GetData());
			var parts = Math.floor(data.length / Module.MAX_DATA_SIZE);
			var lastPart = data.length % Module.MAX_DATA_SIZE;
			for (var j = 0; j < parts; j++) {
				euSign.CtxHashDataContinue(hashContext, new Uint8Array(
					data.buffer, j * Module.MAX_DATA_SIZE, Module.MAX_DATA_SIZE));
			}
			if (lastPart) {
				euSign.CtxHashDataContinue(hashContext, new Uint8Array(
					data.buffer, parts * Module.MAX_DATA_SIZE, lastPart));
			}

			var hash = euSign.CtxHashDataEnd(hashContext, false);
			var useCRL = IsUseCRLs(signerCert.GetInfoEx().GetIssuerCN());
			var signInfo = euSign.VerifyHashOnTimeEx(
				hash, i, sign, null, useCRL, !useCRL);
			var signFormat = euSign.GetSignType(i, sign);
						
			signsInfos.push(CreateSignInfoResult(
				signInfo, signerCert, signFormat, 
				EUSignContainerType.CAdES | EUSignContainerType.Detached,
				true, null));

			euSign.CtxFreeHash(hashContext);
		}

		euSign.CtxFree(context);
	} catch (e) {
		if (context != null)
			euSign.CtxFree(context);
		throw e;
	}

	return signsInfos;
}

//------------------------------------------------------------------------------

function VerifyDataWithInternalSigns(context, signedData, signsInfos) {
	var isInternalSign;
	var signsCount;
	var signInfo;
	var signerCert;
	var useCRL;
	var data;
	var signFormat;

	try {
		isInternalSign = euSign.IsDataInSignedDataAvailable(signedData);
	} catch (e) {
		if (e.errorCode != EU_ERROR_PKI_FORMATS_FAILED)
			throw e;

		isInternalSign = false;
	}

	if (!isInternalSign)
		return null;
	
	signsCount = euSign.CtxGetSignsCount(context, signedData);
	if (signsCount <= 0)
		euSign.RaiseError(EU_ERROR_PKI_FORMATS_FAILED);
	for (var i = 0; i < signsCount; i++) {
		signerCert = euSign.CtxGetSignerInfo(context, i, signedData);
		useCRL = IsUseCRLs(signerCert.GetInfoEx().GetIssuerCN());
		signInfo = euSign.VerifyDataInternalOnTimeEx(
			i, signedData, null, useCRL, !useCRL);
		signFormat = euSign.GetSignType(i, signedData);

		data = signInfo.data;
		signInfo.data = null;
		signsInfos.push(CreateSignInfoResult(
			signInfo, signerCert, signFormat, 
			EUSignContainerType.CAdES | EUSignContainerType.Enveloped, 
			true, null));
	}

	return data;
}

//------------------------------------------------------------------------------

function VerifyMultySignedDataInternal(signedData) {
	var signsInfos = [];

	var _tmpData = signedData;
	var _signsInfos = [];
	var context = null;
	var verifiedData = signedData;

	try {
		context = euSign.CtxCreate();
		euSign.CtxSetParameter(context, EU_RESOLVE_OIDS_PARAMETER, false);
		while((_tmpData = VerifyDataWithInternalSigns(
					context, _tmpData, _signsInfos)) != null) {
			verifiedData = _tmpData;
			signsInfos.unshift(_signsInfos);
			_signsInfos = [];
		}
		euSign.CtxFree(context);
	} catch (e) {
		if (context != null)
			euSign.CtxFree(context);
		throw e;
	}

	return {
		'signsInfos': signsInfos,
		'data': verifiedData
	};
}

//------------------------------------------------------------------------------

function VerifyMultiSignFileDataInternal(signedFile, onSuccess, onError) {
	var _onSuccess = function(signedFileData) {
		try {
			var signsInfos = [];
			var data;
			var signedData = signedFileData.data;

			var result = VerifyMultySignedDataInternal(
				signedData);
			if (result.signsInfos.length == 0) {
				onSuccess(new EUVerifyResult(
					EUVerifyErrorCode.InvalidFileFormat, 
					signedFile, null,
					EUSignContainerType.Unknown, null, null));
				return;
			}
				
			data = result.data;
			for (var i = 0; i < result.signsInfos.length; i++) {
				signsInfos.push(result.signsInfos[i]);
			}

			var signType = EUSignContainerType.Enveloped | 
				EUSignContainerType.CAdES;
			if (signsInfos.length > 1 || 
				(signsInfos.length > 0 && signsInfos[0].length > 1)) {
				signType |= EUSignContainerType.Multi;
			}

			onSuccess(new EUVerifyResult(
				EUVerifyErrorCode.NoError, 
				signedFile, null, signType, 
				signsInfos, data, null));
		} catch (e) {
			onError(e);
			return;
		}
	};

	euSign.ReadFile(signedFile, _onSuccess, onError);
}

//------------------------------------------------------------------------------

function VerifyBase64FileInternal(signedFile, onSuccess, onError) {
	var _onSuccess = function(signedFileData) {
		var signedData = null;
		try {
			signedData = euSign.Base64Decode(
				ArrayToString(signedFileData.data));
		} catch (e) {
			onSuccess(new EUVerifyResult(
				EUVerifyErrorCode.InvalidFileFormat, 
				signedFile, null,
				EUSignContainerType.Unknown, null, null));
			return;
		}

		try {
			var signsInfos = [];
			var data;

			if (s_verifyMultiSign) {
				var result = VerifyMultySignedDataInternal(
					signedData);
				if (result.signsInfos.length == 0) {
					onSuccess(new EUVerifyResult(
						EUVerifyErrorCode.InvalidFileFormat, 
						signedFile, null,
						EUSignContainerType.Unknown, null, null));
					return;
				}	
					
				data = result.data;
				for (var i = 0; i < result.signsInfos.length; i++) {
					signsInfos.push(result.signsInfos[i]);
				}
			} else {
				var context = euSign.CtxCreate();
				euSign.CtxSetParameter(context, 
					EU_RESOLVE_OIDS_PARAMETER, false);

				var signerCert = euSign.CtxGetSignerInfo(
					context, 0, signedData);
				var useCRL = IsUseCRLs(
					signerCert.GetInfoEx().GetIssuerCN());
				var signInfo = euSign.VerifyDataInternalOnTimeEx(
					0, signedData, null, useCRL, !useCRL);
				var signFormat = euSign.GetSignType(0, signedData);

				data = signInfo.data;
				signInfo.data = null;
				signsInfos.push(CreateSignInfoResult(
					signInfo, signerCert, signFormat, 
					EUSignContainerType.CAdES | EUSignContainerType.Enveloped,
					true, null));
				euSign.CtxFree(context);
			}

			var signType = EUSignContainerType.Enveloped | 
				EUSignContainerType.CAdES | EUSignContainerType.Base64;
			if (signsInfos.length > 1 || 
				(signsInfos.length > 0 && signsInfos[0].length > 1)) {
				signType |= EUSignContainerType.Multi;
			}

			onSuccess(new EUVerifyResult(
				EUVerifyErrorCode.NoError, 
				signedFile, null, signType, 
				signsInfos, data, null));
		} catch (e) {
			onError(e);
			return;
		}
	};
	
	euSign.ReadFile(signedFile, _onSuccess, onError);
}

//------------------------------------------------------------------------------

function VerifyMultiSignFile(file, fileWithSign, onSuccess, onError) {
	var context = null;

	var _getFileSignParams = function(signIndex, 
		onSuccess, onError) {
		euSign.CtxGetFileSignerInfo(
			context, signIndex, fileWithSign, 
			function(signerCert) {
				euSign.GetFileSignType(signIndex, fileWithSign,
					function(signType) {
						onSuccess(signerCert, signType);
					},
					function(e) {
						onError(e);
					});
			},
			function(e) {
				onError(e);
			});
	};

	var _verify = function(signIndex, count, signsInfos) {
		if (signIndex < count) {
			_getFileSignParams(signIndex, 
				function(signerCert, signType) {
					var useCRL = IsUseCRLs(
						signerCert.GetInfoEx().GetIssuerCN());
					euSign.VerifyFileOnTimeEx(signIndex,
						fileWithSign, file, null, useCRL, !useCRL,
						function(signInfo) {
							signsInfos.push(CreateSignInfoResult(
								signInfo, signerCert, signType,
								EUSignContainerType.CAdES |
								(file == null ? 
									EUSignContainerType.Enveloped : 
									EUSignContainerType.Detached),
								true, null));
							_verify(signIndex + 1, count, signsInfos);
						},
						function(e) {
							onError(e);
						});
				},
				function(e) {
					onError(e);
				});
			return;
		}
		
		var signType = (file == null) ? 
			EUSignContainerType.Enveloped : EUSignContainerType.Detached;
		if (signsInfos.length > 1) {
			signType |= EUSignContainerType.Multi;
		}
		signType |= EUSignContainerType.CAdES;

		onSuccess(new EUVerifyResult(
			EUVerifyErrorCode.NoError, 
			fileWithSign, file, signType, 
			[signsInfos], null, null));
	};

	try {
		context = euSign.CtxCreate();
		euSign.CtxSetParameter(context, 
			EU_RESOLVE_OIDS_PARAMETER, false);
					
		euSign.CtxGetFileSignsCount(
			context, fileWithSign, 
			function(count) {
				if (count <= 0) {
					onError(euSign.MakeError(
						EU_ERROR_PKI_FORMATS_FAILED));
					return;
				}

				var signsInfos = [];
				
				_verify(0, count, signsInfos);
			}, function (e) {
				onError(e);
			});		
	} catch (e) {
		if (context != null)
			euSign.CtxFree(context);
		
		onError(e);
		return;
	}
}

//------------------------------------------------------------------------------

function VerifyFileWithInternalSign(file, onSuccess, onError) {
	if (file == null) {
		onError(euSign.MakeError(EU_ERROR_BAD_PARAMETER));
		return;
	}

	var _onSuccess = function(isAvailable) {
		if (!isAvailable) {
			onSuccess(new EUVerifyResult(
				EUVerifyErrorCode.NoFileWithData, 
				file, null, 
				EUSignContainerType.Detached | EUSignContainerType.CAdES,
				null, null, null));
			return;
		}

		if (s_verifyMultiSign && file.size <= s_maxFileSize) {
			VerifyMultiSignFileDataInternal(file, onSuccess, onError);
			return;
		}

		VerifyMultiSignFile(null, file, 
			onSuccess, onError);
	};
	
	var _onError = function(e) {
		if (e.errorCode != EU_ERROR_PKI_FORMATS_FAILED) {
			onError(e);
			return;
		}

		if (!s_verifyBase64Sign || (file.size > s_maxFileSize)) {
			onSuccess(new EUVerifyResult(
				EUVerifyErrorCode.InvalidFileFormat, 
				file, null, 
				EUSignContainerType.Unknown, null, null, null));
			return;
		}

		VerifyBase64FileInternal(
			file, onSuccess, onError);
	};

	euSign.IsDataInSignedFileAvailable(
		file, _onSuccess, _onError);
}

//------------------------------------------------------------------------------

function VerifyFileWithExternalSign(file, fileWithSign, onSuccess, onError) {
	if (file == null || fileWithSign == null) {
		onError(euSign.MakeError(EU_ERROR_BAD_PARAMETER));
		return;
	}

	var _onSuccess = function(isAvailable) {
		if (isAvailable) {
			onSuccess(new EUVerifyResult(
				EUVerifyErrorCode.InvalidFileFormat, 
				fileWithSign, file,
				EUSignContainerType.Detached | EUSignContainerType.CAdES,
				null, null, null));
			return;
		}
		
		VerifyMultiSignFile(file, fileWithSign, 
			onSuccess, onError);
	};

	euSign.IsDataInSignedFileAvailable(
		fileWithSign, _onSuccess, onError);
}

//------------------------------------------------------------------------------

function VerifyFileWithASiC(file, onSuccess, onError) {
	var _onInvalidFormat = function() {
		onSuccess(new EUVerifyResult(
			EUVerifyErrorCode.InvalidFileFormat, 
			null, file,
			EUSignContainerType.Unknown, null, null, null));
	};
	
	var fileData = null;
	try {
		fileData = euSign.ReadFile(file).GetData();
	} catch (e) {
		_onInvalidFormat();
		return;
	}

	var signContainerType = EUSignContainerType.Unknown;
	var context = null;

	try {
		context = euSign.CtxCreate();
		euSign.CtxSetParameter(context, EU_RESOLVE_OIDS_PARAMETER, false);

		var signsInfos = [];
		var allDatasNames = [];
		var datas = [];

		var asicType = euSign.ASiCGetASiCType(fileData);
		var asicSignType = euSign.ASiCGetSignType(fileData);
		var signsCount = euSign.ASiCGetSignsCount(fileData);
		if (signsCount <= 0)
			euSign.RaiseError(EU_ERROR_PKI_FORMATS_FAILED);

		signContainerType = (asicType == EU_ASIC_TYPE_S) ? 
			EUSignContainerType.ASiCS : EUSignContainerType.ASiCE;
		signContainerType |= (asicSignType == EU_ASIC_SIGN_TYPE_CADES) ? 
			EUSignContainerType.CAdES : EUSignContainerType.XAdES;

		for (var i = 0; i < signsCount; i++) {
			var signInfo = euSign.ASiCVerifyData(i, fileData);
			var signerCert = euSign.CtxASiCGetSignerInfo(
				context, i, fileData);
			var signFormat = euSign.ASiCGetSignLevel(i, fileData);
			var isAllContentCovered = euSign.ASiCIsAllContentCovered(
				i, fileData);
	
			var refNames = euSign.ASiCGetSignReferences(i, fileData);
			for (var j = 0; j < refNames.length; j++) {
				if (refNames[j].length > 0 && refNames[j][0] == '#')
					continue;

				if (allDatasNames[refNames[j]])
					continue;

				allDatasNames[refNames[j]] = true;

				var refData = euSign.ASiCGetReference(fileData, refNames[j]);
				datas.push({
					name: refNames[j],
					val: refData
				});
			}
			signsInfos.push(CreateSignInfoResult(
				signInfo, signerCert, signFormat, 
				signContainerType, isAllContentCovered, refNames));
		}

		euSign.CtxFree(context);

		onSuccess(new EUVerifyResult(
			EUVerifyErrorCode.NoError, 
			file, null, signContainerType, 
			[signsInfos], null, datas));
	} catch (e) {
		if (context != null)
			euSign.CtxFree(context);

		onError(e);
		return;
	}
}

//------------------------------------------------------------------------------

function GetFileExtension(file) {
	return file.name.split('.').pop().toLowerCase();
}

//------------------------------------------------------------------------------

function IsASiC(file) {
	return ASIC_S_FILES_EXTENSIONS.indexOf(GetFileExtension(file)) != -1;
}

//------------------------------------------------------------------------------

function IsPAdES(file) {
	try {
		if (GetFileExtension(file) != 'pdf')
			return false;

		var fileData = euSign.ReadFile(file).GetData();
		var signsCount = euSign.PDFGetSignsCount(fileData);
		return signsCount > 0;
	} catch (e) {
		return false;
	}
}

//------------------------------------------------------------------------------

function IsXAdES(file) {
	try {
		if (GetFileExtension(file) != 'xml')
			return false;

		var fileData = euSign.ReadFile(file).GetData();
		var signsCount = euSign.XAdESGetSignsCount(fileData);
		return signsCount > 0;
	} catch (e) {
		return false;
	}
}

//------------------------------------------------------------------------------

function VerifyFileWithPDF(file, onSuccess, onError) {
	var _onInvalidFormat = function() {
		onSuccess(new EUVerifyResult(
			EUVerifyErrorCode.InvalidFileFormat, 
			null, file,
			EUSignContainerType.Unknown, null, null, null));
	};
	
	var fileData = null;
	try {
		fileData = euSign.ReadFile(file).GetData();
	} catch (e) {
		_onInvalidFormat();
		return;
	}

	var signContainerType = EUSignContainerType.Unknown;
	var context = null;

	try {
		context = euSign.CtxCreate();
		euSign.CtxSetParameter(context, EU_RESOLVE_OIDS_PARAMETER, false);

		var signsInfos = [];

		var signsCount = euSign.PDFGetSignsCount(fileData);
		if (signsCount <= 0)
			euSign.RaiseError(EU_ERROR_PKI_FORMATS_FAILED);
		signContainerType = EUSignContainerType.PAdES;

		for (var i = 0; i < signsCount; i++) {
			var signInfo = euSign.PDFVerifyData(i, fileData);
			var signerCert = euSign.CtxPDFGetSignerInfo(
				context, i, fileData);
			var signType = euSign.PDFGetSignType(i, fileData);
	
			signInfo.data = fileData;
			signsInfos.push(CreateSignInfoResult(
				signInfo, signerCert, signType, 
				signContainerType, true, null));
		}

		euSign.CtxFree(context);

		onSuccess(new EUVerifyResult(
			EUVerifyErrorCode.NoError, 
			file, null, signContainerType, 
			[signsInfos], fileData, null));
	} catch (e) {
		if (context != null)
			euSign.CtxFree(context);

		onError(e);
		return;
	}
}

//------------------------------------------------------------------------------

function VerifyFileWithXAdES(dataFile, signFile, onSuccess, onError) {
	var _onInvalidFormat = function() {
		onSuccess(new EUVerifyResult(
			EUVerifyErrorCode.InvalidFileFormat, 
			null, file,
			EUSignContainerType.Unknown, null, null, null));
	};
	
	var data = null;
	var sign = null;

	try {
		if (dataFile)
			data = euSign.ReadFile(dataFile).GetData();
		sign = euSign.ReadFile(signFile).GetData();
	} catch (e) {
		_onInvalidFormat();
		return;
	}

	var signContainerType = EUSignContainerType.Unknown;
	var context = null;

	try {
		context = euSign.CtxCreate();
		euSign.CtxSetParameter(context, EU_RESOLVE_OIDS_PARAMETER, false);

		var signsInfos = [];

		var xadesType = euSign.XAdESGetType(sign);
		if (xadesType == EU_XADES_TYPE_DETACHED && data == null) {
			onSuccess(new EUVerifyResult(
				EUVerifyErrorCode.NoFileWithData, 
				signFile, null, 
				EUSignContainerType.Detached | EUSignContainerType.XAdES,
				null, null, null));
			return;
		}

		var signsCount = euSign.XAdESGetSignsCount(sign);
		if (signsCount <= 0)
			euSign.RaiseError(EU_ERROR_PKI_FORMATS_FAILED);

		signContainerType = EUSignContainerType.XAdES;
		switch (xadesType) {
			case EU_XADES_TYPE_DETACHED:
				signContainerType |= EUSignContainerType.Detached;
				break;
			case EU_XADES_TYPE_ENVELOPED:
				signContainerType |= EUSignContainerType.Enveloped;
				break;
			case EU_XADES_TYPE_ENVELOPING:
				signContainerType |= EUSignContainerType.Enveloping;
				break;
		}

		var references = null;
		if (xadesType == EU_XADES_TYPE_DETACHED) {
			references = [];

			references.push(new EndUserReference(
				dataFile.name, data));
		}	

		for (var i = 0; i < signsCount; i++) {
			var signInfo = euSign.XAdESVerifyData(references, i, sign);
			var signerCert = euSign.CtxXAdESGetSignerInfo(
				context, i, sign);
			var signType = euSign.XAdESGetSignLevel(i, sign);
	
			var dataReferences = references;
			/*
			if (dataReferences == null) {
				dataReferences = [];
				var dataReferencesNames = euSign.XAdESGetSignReferences(
					i, sign);
				for (var j = 0; j < dataReferencesNames.length; j++) {
					var referenceName = dataReferencesNames[j];
					var referenceData = euSign.XAdESGetReference(
						sign, referenceName);
					dataReferences.push(new EndUserReference(
						referenceName, referenceData));
				}
			}
			*/

			signInfo.data = dataReferences;
			signsInfos.push(CreateSignInfoResult(
				signInfo, signerCert, signType, 
				signContainerType, true, null));
		}

		euSign.CtxFree(context);

		onSuccess(new EUVerifyResult(
			EUVerifyErrorCode.NoError, 
			signFile, dataFile, signContainerType, 
			[signsInfos], data != null ? data : sign, null)); // !!!!
	} catch (e) {
		if (context != null)
			euSign.CtxFree(context);

		onError(e);
		return;
	}
}

//------------------------------------------------------------------------------

function VerifyFilesWithExternalSign(files, onSuccess, onError) {
	if (files[0] == null || files[1] == null) {
		onError(euSign.MakeError(EU_ERROR_BAD_PARAMETER));
		return;
	}

	var _onError = function(e) {
		if (e.errorCode != EU_ERROR_PKI_FORMATS_FAILED) {
			onError(e);
			return;
		}

		VerifyFileWithExternalSign(
			files[1], files[0], onSuccess, 
			function(e) {
				if (e.errorCode != EU_ERROR_PKI_FORMATS_FAILED) {
					onError(e);
					return;
				}
				
				onSuccess(new EUVerifyResult(
					EUVerifyErrorCode.InvalidFileFormat, 
					files[0], files[1],
					EUSignContainerType.Detached, null, null, null));
		});
	};

	VerifyFileWithExternalSign(
		files[0], files[1], onSuccess, _onError);
}

//------------------------------------------------------------------------------

function VerifyFiles(data) {
	var files = data.params.files;

	if ((files == null) || (files.length <= 0)) {
		sendMessage(data, 
			euSign.MakeError(EU_ERROR_BAD_PARAMETER), null);
		return;
	}

	var _onSuccess = function(result) {
		var params = {'result': result};
		sendMessage(data, null, params);
		return;
	};

	var _onError = function(e) {
		sendMessage(data, e, null);
	};

	if (!s_verifyLargeFiles) {
		if (files[0].size > s_maxFileSize) {
			_onSuccess(new EUVerifyResult(
				EUVerifyErrorCode.FileTooBig, 
				files[0], null, 
				EUSignContainerType.Unknown, null, null, null));
			return;
		}

		if (files.length == 2 && 
			files[1].size > s_maxFileSize) {
			_onSuccess(new EUVerifyResult(
				EUVerifyErrorCode.FileTooBig, 
				files[1], null, 
				EUSignContainerType.Unknown, null, null, null));
			return;
		}
	}
	
	if (files.length == 1) {
		if (IsASiC(files[0])) {
			VerifyFileWithASiC(
				files[0], _onSuccess, _onError);
		} else if (IsPAdES(files[0])) {
			VerifyFileWithPDF(
				files[0], _onSuccess, _onError);
		} else if (IsXAdES(files[0])) {
			VerifyFileWithXAdES(
				null, files[0], _onSuccess, _onError);
		} else {
			VerifyFileWithInternalSign(
				files[0], _onSuccess, _onError);
		}
	} else {
		var xadesDataFile = null, xadesSignFile = null;
		if (IsXAdES(files[0])) {
			xadesDataFile = files[1];
			xadesSignFile = files[0];
		} else if (IsXAdES(files[1])) {
			xadesDataFile = files[0];
			xadesSignFile = files[1];
		}

		if (xadesDataFile != null && xadesSignFile != null) {
			VerifyFileWithXAdES(
				xadesDataFile, xadesSignFile, _onSuccess, _onError);
		} else {
			VerifyFilesWithExternalSign(
				files, _onSuccess, _onError);
		}
	}
}

//==============================================================================

onmessage = function(e) {
	if (!s_loaded) {
		setTimeout(function() {
				onmessage(e);
			}, 100);
		return;
	}

	var data = e.data;

	switch (data.cmd) {
		case 'Initialize':
			Initialize(data);
			break;

		case 'GetDataFromSignedFile':
			GetDataFromSignedFile(data);
			break;

		case 'VerifyFiles':
			VerifyFiles(data);
			break;

		default:
			var error = euSign.MakeError(EU_ERROR_NOT_SUPPORTED);
			sendMessage(data.cmd, data, error, null);
			break;
	}
};

//==============================================================================

function EUSignCPModuleInitialized(isInitialized) {
	s_loaded = isInitialized;
}

var euSign = EUSignCP();

//==============================================================================

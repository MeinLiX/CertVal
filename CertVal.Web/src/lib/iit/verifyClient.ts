/**
 * Promise-based wrapper around the locally-hosted IIT verification worker
 * (static/iit/src/eusignverify.worker.js).
 *
 * Worker protocol differs from the sign worker:
 *   send:    { cmd, callback_id, params: {...} }
 *   receive: { cmd, callback_id, error: {errorCode, message}|null, params: {...}|null }
 */

export interface VerifySettings {
	language: string;
	httpProxyServiceURL?: string;
	directAccess: boolean;
	CAs: string | unknown[];
	CACertificates: string | Uint8Array;
	TSLAddress?: string;
	verifySettings: {
		signInfoTmpl: string | Uint8Array;
		ocspResponseExpireTime?: number;
		supportAdvancedCertificates?: boolean;
		maxFileSize?: { default: number; mobile: number };
	};
	CRLs?: unknown[];
}

export interface EUVerifySignerInfo {
	subjCN?: string;
	subjOrg?: string;
	subjFullName?: string;
	subjTitle?: string;
	subjEMail?: string;
	subjDRFOCode?: string;
	subjEDRPOUCode?: string;
	issuerCN?: string;
	serial?: string;
	certBeginTime?: string;
	certEndTime?: string;
	[key: string]: unknown;
}

export interface EUVerifyTimeInfo {
	isTimeAvail?: boolean;
	isTimeStamp?: boolean;
	time?: string;
	[key: string]: unknown;
}

export interface EUVerifySignInfo {
	signerInfo: EUVerifySignerInfo;
	signTimeInfo: EUVerifyTimeInfo;
	isDigitalStamp: boolean;
	qscd: { use: boolean; name?: string; sn?: string };
	signAlgoType: number;
	signAlgo: string;
	signFormatType: number;
	signFormat: string;
	signContainerType: number;
	signContainer: string;
	isAllContentCovered: boolean;
	signReferences: unknown;
}

export interface EUVerifyResult {
	resultCode: number;
	signFile: File | null;
	dataFile: File | null;
	signType: number;
	signsInfos: (EUVerifySignInfo | EUVerifySignInfo[])[] | null;
	data: Uint8Array | null;
	datas: { name: string; val: Uint8Array }[] | null;
}

interface PendingCall {
	resolve: (value: unknown) => void;
	reject: (reason: unknown) => void;
}

interface VerifyResponse {
	cmd: string;
	callback_id: number;
	error: { errorCode?: number; message?: string } | null;
	params: Record<string, unknown> | null;
}

export class VerifyError extends Error {
	constructor(message: string, public readonly errorCode?: number) {
		super(message);
		this.name = 'VerifyError';
	}
}

export class VerifyClient {
	private worker: Worker | null = null;
	private nextId = 1;
	private readonly pending = new Map<number, PendingCall>();
	private initialized = false;

	start(): void {
		if (this.worker) return;
		this.worker = new Worker('/iit/src/eusignverify.worker.js');
		this.worker.onmessage = (e: MessageEvent<VerifyResponse>) => {
			const data = e.data;
			if (!data || typeof data.callback_id !== 'number') return;
			const p = this.pending.get(data.callback_id);
			if (!p) return;
			this.pending.delete(data.callback_id);
			if (data.error) p.reject(new VerifyError(data.error.message ?? 'Unknown verify error', data.error.errorCode));
			else p.resolve(data.params);
		};
		this.worker.onerror = (e) => {
			for (const [, p] of this.pending) p.reject(new Error(e.message || 'Verify worker error'));
			this.pending.clear();
		};
	}

	terminate(): void {
		this.worker?.terminate();
		this.worker = null;
		this.initialized = false;
	}

	private call<T>(cmd: string, params: Record<string, unknown>): Promise<T> {
		this.start();
		const callback_id = this.nextId++;
		return new Promise<T>((resolve, reject) => {
			this.pending.set(callback_id, { resolve: resolve as (v: unknown) => void, reject });
			this.worker!.postMessage({ cmd, callback_id, params });
		});
	}

	async initialize(settings: VerifySettings): Promise<unknown> {
		const result = await this.call('Initialize', {
			origin: window.location.origin,
			pathname: window.location.pathname,
			settings
		});
		this.initialized = true;
		return result;
	}

	isInitialized(): boolean {
		return this.initialized;
	}

	async verifyFiles(files: File[]): Promise<EUVerifyResult> {
		const params = await this.call<{ result: EUVerifyResult }>('VerifyFiles', { files });
		return params.result;
	}

	getDataFromSignedFile(signedFile: File): Promise<{ data: Uint8Array }> {
		return this.call<{ data: Uint8Array }>('GetDataFromSignedFile', { signedFile });
	}
}

/* Container type bitmask (mirrors EUSignContainerType in worker). */
export const EUSignContainerType = {
	Unknown: 0x0000,
	Detached: 0x0001,
	Enveloped: 0x0002,
	Enveloping: 0x0004,
	Multi: 0x0008,
	Base64: 0x0010,
	ASiCS: 0x0020,
	ASiCE: 0x0040,
	CAdES: 0x0080,
	XAdES: 0x0100,
	PAdES: 0x0200
} as const;

/* Flatten nested signsInfos (some shapes are EUVerifySignInfo[][]) into a single list. */
export function flattenSignsInfos(
	signsInfos: (EUVerifySignInfo | EUVerifySignInfo[])[] | null
): EUVerifySignInfo[] {
	if (!signsInfos) return [];
	const out: EUVerifySignInfo[] = [];
	for (const item of signsInfos) {
		if (Array.isArray(item)) out.push(...item);
		else out.push(item);
	}
	return out;
}

/**
 * Thin Promise-based wrapper around the locally-hosted IIT EUSignCP signing
 * worker (static/iit/src/euscp.worker.js).
 *
 * The worker accepts messages of the form
 *     { id, cmd, params: [...], origin, pathname }
 * and responds with
 *     { id, cmd, result, error }
 * Events (such as sign certificates being unlocked) arrive with id === -2.
 */

export interface JKSPrivateKey {
	alias: string;
	privateKey: Uint8Array;
	certificates: unknown[];
	digitalStamp: boolean;
}

export interface SignSettings {
	language?: string;
	encoding?: string; // 'UTF-8' | 'UTF-16LE'
	CAs?: string | unknown[];
	CACertificates?: string | Uint8Array;
	httpProxyServiceURL?: string;
	directAccess?: boolean;
	TSLAddress?: string;
	[key: string]: unknown;
}

interface PendingCall {
	resolve: (value: unknown) => void;
	reject: (reason: unknown) => void;
}

interface WorkerResponse {
	id: number;
	cmd: string | null;
	result: unknown;
	error: { errorCode?: number; message?: string } | null;
}

export class SignClient {
	private worker: Worker | null = null;
	private nextId = 1;
	private readonly pending = new Map<number, PendingCall>();
	private ready = false;

	/** Lazily start the worker. */
	start(): void {
		if (this.worker) return;
		this.worker = new Worker('/iit/src/euscp.worker.js');
		this.worker.onmessage = (e: MessageEvent<WorkerResponse>) => {
			const data = e.data;
			if (!data || typeof data.id !== 'number') return;
			if (data.id === -2) return; // event broadcast – ignored
			const p = this.pending.get(data.id);
			if (!p) return;
			this.pending.delete(data.id);
			if (data.error) p.reject(new SignError(data.error.message ?? 'Unknown error', data.error.errorCode));
			else p.resolve(data.result);
		};
		this.worker.onerror = (e) => {
			// Reject every outstanding call so the UI does not hang.
			for (const [, p] of this.pending) p.reject(new Error(e.message || 'Sign worker error'));
			this.pending.clear();
		};
	}

	terminate(): void {
		this.worker?.terminate();
		this.worker = null;
		this.ready = false;
		for (const [, p] of this.pending) p.reject(new Error('Worker terminated'));
		this.pending.clear();
	}

	private call<T>(cmd: string, params: unknown[] = []): Promise<T> {
		this.start();
		const id = this.nextId++;
		return new Promise<T>((resolve, reject) => {
			this.pending.set(id, { resolve: resolve as (v: unknown) => void, reject });
			this.worker!.postMessage({
				id,
				cmd,
				params,
				origin: window.location.origin,
				pathname: window.location.pathname
			});
		});
	}

	async initialize(settings: SignSettings): Promise<unknown> {
		const result = await this.call('Initialize', [settings]);
		this.ready = true;
		return result;
	}

	isReady(): boolean {
		return this.ready;
	}

	setRuntimeParameter(name: string, value: unknown): Promise<void> {
		return this.call<void>('SetRuntimeParameter', [name, value]);
	}

	readPrivateKeyBinary(keyBytes: Uint8Array, password: string, certs?: unknown[] | null): Promise<unknown> {
		// Signature: ReadPrivateKeyBinary(privateKey, password, certs, CACommonName, onSuccess, onError)
		// Worker dispatch appends [onSuccess, onError] to params, so supply null for certs & CACommonName.
		return this.call('ReadPrivateKeyBinary', [keyBytes, password, certs ?? null, null]);
	}

	/** Enumerate private keys inside a JKS container. */
	getJKSPrivateKeys(jksBytes: Uint8Array): Promise<JKSPrivateKey[]> {
		return this.call<JKSPrivateKey[]>('GetJKSPrivateKeys', [jksBytes]);
	}

	isPrivateKeyReaded(): Promise<boolean> {
		return this.call<boolean>('IsPrivateKeyReaded', []);
	}

	resetPrivateKey(): Promise<void> {
		return this.call<void>('ResetPrivateKey', []);
	}

	/**
	 * Wraps SignDataEx.
	 * Returns a Uint8Array (the signed CMS / CAdES container).
	 */
	signDataEx(
		signAlgo: number,
		data: Uint8Array,
		external: boolean,
		appendCert: boolean,
		asBase64: boolean
	): Promise<Uint8Array | string> {
		return this.call<Uint8Array | string>('SignDataEx', [signAlgo, data, external, appendCert, asBase64]);
	}

	/**
	 * Wraps ASiCSignData.
	 * Returns { name: 'filename.asice' | 'filename.asics', val: Uint8Array }.
	 * Worker signature: ASiCSignData(signAlgo, asicType, signType, signLevel, references, asBase64String)
	 */
	asicSignData(
		signAlgo: number,
		asicType: number,
		asicSignType: number,
		signLevel: number,
		fileName: string,
		fileData: Uint8Array,
		asBase64: boolean
	): Promise<{ name: string; val: Uint8Array }> {
		return this.call<{ name: string; val: Uint8Array }>('ASiCSignData', [
			signAlgo,
			asicType,
			asicSignType,
			signLevel,
			[{ name: fileName, val: fileData }],
			asBase64
		]);
	}

	/**
	 * Wraps XAdESSignData.
	 * Returns { name: 'filename.xml', val: Uint8Array }.
	 * Worker signature: XAdESSignData(signAlgo, xadesType, signLevel, references, asBase64String)
	 */
	xadesSignData(
		signAlgo: number,
		xadesType: number,
		signLevel: number,
		fileName: string,
		fileData: Uint8Array,
		asBase64: boolean
	): Promise<{ name: string; val: Uint8Array }> {
		return this.call<{ name: string; val: Uint8Array }>('XAdESSignData', [
			signAlgo,
			xadesType,
			signLevel,
			[{ name: fileName, val: fileData }],
			asBase64
		]);
	}

	/**
	 * Wraps PDFSignData — signs a PDF file in-place (PAdES).
	 * Returns { name: string, val: Uint8Array } (signed PDF bytes, same filename).
	 * Worker signature: PDFSignData(signAlgo, pdfData, signType, asBase64String)
	 */
	pdfSignData(
		signAlgo: number,
		fileName: string,
		pdfData: Uint8Array,
		signType: number,
		asBase64: boolean
	): Promise<{ name: string; val: Uint8Array }> {
		return this.call<{ name: string; val: Uint8Array }>('PDFSignData', [
			signAlgo,
			{ name: fileName, val: pdfData },
			signType,
			asBase64
		]);
	}

	getOwnCertificates(): Promise<unknown[]> {
		return this.call<unknown[]>('GetOwnCertificates', []);
	}

	/**
	 * Wraps SignHash — signs a pre-computed hash value.
	 * Returns the raw signature bytes (Uint8Array) or base64 string.
	 * Worker signature: SignHash(signAlgo, hash, appendCert, asBase64String)
	 */
	signHash(
		signAlgo: number,
		hash: Uint8Array,
		appendCert: boolean,
		asBase64: boolean
	): Promise<Uint8Array | string> {
		return this.call<Uint8Array | string>('SignHash', [signAlgo, hash, appendCert, asBase64]);
	}

	/**
	 * Wraps HashData — computes hash of data.
	 * Returns Uint8Array or base64 string.
	 * Worker signature: HashData(hashAlgo, data, asBase64String)
	 */
	hashData(
		hashAlgo: number,
		data: Uint8Array,
		asBase64: boolean
	): Promise<Uint8Array | string> {
		return this.call<Uint8Array | string>('HashData', [hashAlgo, data, asBase64]);
	}
}

export class SignError extends Error {
	constructor(message: string, public readonly errorCode?: number) {
		super(message);
		this.name = 'SignError';
	}
}

/* CAdES sign-type bitmask values (mirrored from euscp.worker.js). */
export const EU_SIGN_TYPE_CADES_BES = 1;
export const EU_SIGN_TYPE_CADES_T = 4;
export const EU_SIGN_TYPE_CADES_C = 8;
export const EU_SIGN_TYPE_CADES_X_LONG = 16;
export const EU_SIGN_TYPE_CADES_X_LONG_TRUSTED = 128;

export const EU_SIGN_TYPE_PARAMETER = 'SignType';

/* ASiC type constants. */
export const EU_ASIC_TYPE_S = 1;
export const EU_ASIC_TYPE_E = 2;

/* ASiC sign type constants. */
export const EU_ASIC_SIGN_TYPE_CADES = 1;
export const EU_ASIC_SIGN_TYPE_XADES = 2;

/* ASiC sign level constants. */
export const EU_ASIC_SIGN_LEVEL_BES = 1;
export const EU_ASIC_SIGN_LEVEL_T = 4;

/* XAdES type constants. */
export const EU_XADES_TYPE_DETACHED = 1;
export const EU_XADES_TYPE_ENVELOPING = 2;
export const EU_XADES_TYPE_ENVELOPED = 3;

/* XAdES sign level constants. */
export const EU_XADES_SIGN_LEVEL_B_B = 1;
export const EU_XADES_SIGN_LEVEL_B_T = 4;
export const EU_XADES_SIGN_LEVEL_B_LT = 16;

/* Sign algorithm constants (EU_CTX_SIGN_*). 0 = UNKNOWN — picks default from key. */
export const EU_CTX_SIGN_UNKNOWN = 0;

/* Runtime parameter names. */
export const EU_CHECK_CERT_CHAIN_ON_SIGN_TIME_PARAMETER = 'CheckCertChainOnSignTime';

/* Hash algorithm constants (EU_CTX_HASH_ALGO_*). Mirrored from euscpt.js. */
export const EU_CTX_HASH_ALGO_UNKNOWN      = 0;  // auto-detect from loaded key
export const EU_CTX_HASH_ALGO_GOST34311    = 1;
export const EU_CTX_HASH_ALGO_SHA256       = 4;
export const EU_CTX_HASH_ALGO_SHA384       = 5;
export const EU_CTX_HASH_ALGO_SHA512       = 6;
export const EU_CTX_HASH_ALGO_DSTU7564_256 = 7;
export const EU_CTX_HASH_ALGO_DSTU7564_384 = 8;
export const EU_CTX_HASH_ALGO_DSTU7564_512 = 9;

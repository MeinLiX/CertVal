/**
 * Browser-side file helpers used by the /utils signing & verification tools.
 *
 * Everything in this module is intentionally framework-agnostic and only
 * touches the DOM at call time (so it is safe to import from SSR-disabled
 * pages — it will not run on the server because the pages set ssr=false).
 */

/** Trigger a download for the given Blob with the supplied filename. */
export function downloadBlob(blob: Blob, filename: string): void {
	const url = URL.createObjectURL(blob);
	const a = document.createElement('a');
	a.href = url;
	a.download = filename;
	document.body.appendChild(a);
	a.click();
	a.remove();
	// Allow the browser a tick to start the download before revoking.
	setTimeout(() => URL.revokeObjectURL(url), 1000);
}

/** Convenience: download a UTF-8 text/JSON report. */
export function downloadText(text: string, filename: string, mime = 'text/plain;charset=utf-8'): void {
	downloadBlob(new Blob([text], { type: mime }), filename);
}

/** Read the raw bytes of a File as a Uint8Array. */
export function readFileAsUint8Array(file: File): Promise<Uint8Array> {
	return new Promise((resolve, reject) => {
		const reader = new FileReader();
		reader.onload = () => {
			const buf = reader.result as ArrayBuffer;
			resolve(new Uint8Array(buf));
		};
		reader.onerror = () => reject(reader.error ?? new Error('read error'));
		reader.readAsArrayBuffer(file);
	});
}

/** Human-readable file size. */
export function formatBytes(bytes: number): string {
	if (!Number.isFinite(bytes) || bytes < 0) return '—';
	if (bytes === 0) return '0 B';
	const units = ['B', 'KB', 'MB', 'GB'];
	const i = Math.min(units.length - 1, Math.floor(Math.log10(bytes) / 3));
	return `${(bytes / Math.pow(1000, i)).toFixed(i === 0 ? 0 : 1)} ${units[i]}`;
}

/* ---------------------------------------------------------------------------
 * Minimal store-only ZIP builder (no compression).
 *
 * The IIT scripts already ship JSZip into static/iit/verify/lib/, but loading
 * it into the main thread would mean wiring up another global script tag.
 * For the only thing we need on the main page — packaging the produced
 * signatures into a single ZIP for download — a tiny store-only writer is
 * more than enough and keeps the page free of additional dependencies.
 * ------------------------------------------------------------------------- */

const CRC_TABLE: Uint32Array = (() => {
	const t = new Uint32Array(256);
	for (let n = 0; n < 256; n++) {
		let c = n;
		for (let k = 0; k < 8; k++) c = (c & 1) ? (0xedb88320 ^ (c >>> 1)) : (c >>> 1);
		t[n] = c >>> 0;
	}
	return t;
})();

function crc32(bytes: Uint8Array): number {
	let c = 0xffffffff;
	for (let i = 0; i < bytes.length; i++) c = CRC_TABLE[(c ^ bytes[i]) & 0xff] ^ (c >>> 8);
	return (c ^ 0xffffffff) >>> 0;
}

function dosTime(d: Date): { time: number; date: number } {
	const time = ((d.getHours() & 0x1f) << 11) | ((d.getMinutes() & 0x3f) << 5) | ((d.getSeconds() >> 1) & 0x1f);
	const date = (((d.getFullYear() - 1980) & 0x7f) << 9) | (((d.getMonth() + 1) & 0xf) << 5) | (d.getDate() & 0x1f);
	return { time, date };
}

export interface ZipEntry {
	name: string;
	data: Uint8Array;
}

/** Build a store-only ZIP blob. */
export function buildZip(entries: ZipEntry[]): Blob {
	const enc = new TextEncoder();
	const now = new Date();
	const { time, date } = dosTime(now);

	type Prepared = { nameBytes: Uint8Array; data: Uint8Array; crc: number; localOffset: number };
	const prepared: Prepared[] = [];
	const localParts: Uint8Array[] = [];
	let offset = 0;

	for (const e of entries) {
		const nameBytes = enc.encode(e.name);
		const data = e.data;
		const crc = crc32(data);
		const localOffset = offset;

		const header = new Uint8Array(30 + nameBytes.length);
		const dv = new DataView(header.buffer);
		dv.setUint32(0, 0x04034b50, true); // local file header signature
		dv.setUint16(4, 20, true); // version needed
		dv.setUint16(6, 0, true); // flags
		dv.setUint16(8, 0, true); // method: store
		dv.setUint16(10, time, true);
		dv.setUint16(12, date, true);
		dv.setUint32(14, crc, true);
		dv.setUint32(18, data.length, true); // compressed size
		dv.setUint32(22, data.length, true); // uncompressed size
		dv.setUint16(26, nameBytes.length, true);
		dv.setUint16(28, 0, true); // extra length
		header.set(nameBytes, 30);

		localParts.push(header, data);
		prepared.push({ nameBytes, data, crc, localOffset });
		offset += header.length + data.length;
	}

	const centralParts: Uint8Array[] = [];
	let centralSize = 0;
	for (const p of prepared) {
		const cd = new Uint8Array(46 + p.nameBytes.length);
		const dv = new DataView(cd.buffer);
		dv.setUint32(0, 0x02014b50, true);
		dv.setUint16(4, 20, true); // version made by
		dv.setUint16(6, 20, true); // version needed
		dv.setUint16(8, 0, true);
		dv.setUint16(10, 0, true); // store
		dv.setUint16(12, time, true);
		dv.setUint16(14, date, true);
		dv.setUint32(16, p.crc, true);
		dv.setUint32(20, p.data.length, true);
		dv.setUint32(24, p.data.length, true);
		dv.setUint16(28, p.nameBytes.length, true);
		dv.setUint16(30, 0, true);
		dv.setUint16(32, 0, true);
		dv.setUint16(34, 0, true);
		dv.setUint16(36, 0, true);
		dv.setUint32(38, 0, true);
		dv.setUint32(42, p.localOffset, true);
		cd.set(p.nameBytes, 46);
		centralParts.push(cd);
		centralSize += cd.length;
	}

	const eocd = new Uint8Array(22);
	const dv = new DataView(eocd.buffer);
	dv.setUint32(0, 0x06054b50, true);
	dv.setUint16(4, 0, true);
	dv.setUint16(6, 0, true);
	dv.setUint16(8, prepared.length, true);
	dv.setUint16(10, prepared.length, true);
	dv.setUint32(12, centralSize, true);
	dv.setUint32(16, offset, true);
	dv.setUint16(20, 0, true);

	const parts: BlobPart[] = [...localParts, ...centralParts, eocd] as unknown as BlobPart[];
	return new Blob(parts, { type: 'application/zip' });
}

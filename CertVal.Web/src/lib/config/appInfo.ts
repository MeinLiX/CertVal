/**
 * Build-time application metadata.
 *
 * The values are injected by Vite (`define` in vite.config.ts) so they are
 * baked into the bundle and reflect the exact commit the running site was
 * built from.
 */

export const REPO_URL = __APP_REPO_URL__;

export const COMMIT_HASH = __APP_COMMIT__;

/** Whether the build carries a real commit hash (vs. a local "dev" build). */
export const HAS_COMMIT = COMMIT_HASH !== 'dev' && COMMIT_HASH.length > 0;

/** Direct link to the exact commit the site was built from. */
export const COMMIT_URL = HAS_COMMIT ? `${REPO_URL}/commit/${COMMIT_HASH}` : REPO_URL;

import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';
import { execSync } from 'node:child_process';

const REPO_URL = 'https://github.com/MeinLiX/CertVal';

/**
 * Resolves the short commit hash for the current build.
 * Prefers the CI/Docker-provided value, then falls back to the local git
 * checkout, and finally to "dev" when neither is available.
 */
function resolveCommitHash(): string {
	const fromEnv = process.env.VITE_COMMIT_HASH || process.env.PUBLIC_COMMIT_HASH;
	if (fromEnv) {
		return fromEnv.trim().slice(0, 7);
	}

	try {
		return execSync('git rev-parse --short HEAD').toString().trim();
	} catch {
		return 'dev';
	}
}

export default defineConfig({
	base: './',
	define: {
		__APP_COMMIT__: JSON.stringify(resolveCommitHash()),
		__APP_REPO_URL__: JSON.stringify(REPO_URL)
	},
	plugins: [sveltekit()]
});

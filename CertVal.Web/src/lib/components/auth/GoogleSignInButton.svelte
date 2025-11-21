<script lang="ts">
	import { language } from '$lib/stores/language.svelte';
	import { t } from '$lib/i18n';

	let {
		onSuccess,
		onError,
		onLoad,
		'data-test-id': testId = 'google-signin-button'
	} = $props<{
		onSuccess: (token: string) => void;
		onError: (error: string) => void;
		onLoad?: () => void;
		'data-test-id'?: string;
	}>();

	let container = $state<HTMLDivElement>();

	$effect(() => {
		loadGoogleScript(language.current);
	});

	function loadGoogleScript(currentLang: string) {
		if (typeof document === 'undefined') return;

		const scriptId = 'google-identity-script';
		const desiredSrc = `https://accounts.google.com/gsi/client?hl=${currentLang}`;

		let script = document.getElementById(scriptId) as HTMLScriptElement | null;

		if (script) {
			if (script.src === desiredSrc) {
				if (window.google?.accounts?.id) {
					renderButton();
				}
				return;
			} else {
				script.remove();
			}
		}

		script = document.createElement('script');
		script.src = desiredSrc;
		script.async = true;
		script.defer = true;
		script.id = scriptId;
		script.onload = () => {
			renderButton();
		};
		script.onerror = () => {
			if (onLoad) onLoad();
		};
		document.head.appendChild(script);
	}

	function renderButton() {
		const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID;
		if (!clientId) {
			onError(t('errors.googleClientIdMissing', language.current));
			if (onLoad) onLoad();
			return;
		}

		if (!window.google?.accounts?.id || !container) {
			// If google is missing, we wait.
			return;
		}

		try {
			window.google.accounts.id.initialize({
				client_id: clientId,
				callback: handleCredentialResponse,
				auto_select: false,
				ux_mode: 'popup'
			});

			window.google.accounts.id.renderButton(container, {
				type: 'standard',
				theme: 'outline',
				size: 'large',
				text: 'signin_with',
				shape: 'rectangular',
				logo_alignment: 'left',
				width: 320
			});

			if (onLoad) onLoad();
		} catch (e) {
			console.error('Google Sign-In initialization error:', e);
			if (onLoad) onLoad();
		}
	}

	function handleCredentialResponse(response: any) {
		if (response.credential) {
			onSuccess(response.credential);
		} else {
			onError(t('errors.googleLoginFailed', language.current));
		}
	}
</script>

<div bind:this={container} class="w-full" data-test-id={testId}></div>

// See https://svelte.dev/docs/kit/types#app.d.ts
// for information about these interfaces
declare global {
	namespace App {
		// interface Error {}
		// interface Locals {}
		// interface PageData {}
		// interface PageState {}
		// interface Platform {}
	}
	interface Window {
		google?: {
			accounts: {
				id: {
					initialize: (config: {
						client_id: string;
						callback: (response: any) => void;
						auto_select?: boolean;
						ux_mode?: 'popup' | 'redirect';
						login_uri?: string;
						native_callback?: (response: any) => void;
						cancel_on_tap_outside?: boolean;
						prompt_parent_id?: string;
						nonce?: string;
						context?: 'signin' | 'signup' | 'use';
						state_cookie_domain?: string;
						allowed_parent_origin?: string | string[];
						intermediate_iframe_close_callback?: () => void;
						itp_support?: boolean;
					}) => void;
					renderButton: (
						parent: HTMLElement,
						options: {
							type?: 'standard' | 'icon';
							theme?: 'outline' | 'filled_blue' | 'filled_black';
							size?: 'large' | 'medium' | 'small';
							text?: 'signin_with' | 'signup_with' | 'continue_with' | 'signin';
							shape?: 'rectangular' | 'pill' | 'circle' | 'square';
							logo_alignment?: 'left' | 'center';
							width?: number;
							locale?: string;
						}
					) => void;
					prompt: (notification?: (notification: any) => void) => void;
					cancel: () => void;
				};
			};
		};
	}

	/** Short git commit hash injected at build time (see vite.config.ts). */
	const __APP_COMMIT__: string;
	/** Canonical repository URL injected at build time (see vite.config.ts). */
	const __APP_REPO_URL__: string;
}

export { };
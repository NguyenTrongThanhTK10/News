import { api } from './http';

interface CredentialResponse { credential: string; }
interface GoogleIdentityApi {
  initialize(options: { client_id: string; nonce: string; callback: (response: CredentialResponse) => void; auto_select: boolean }): void;
  renderButton(container: HTMLElement, options: { type: 'standard'; theme: 'outline'; size: 'large'; text: 'continue_with'; width: number; locale: string }): void;
}
declare global { interface Window { google?: { accounts: { id: GoogleIdentityApi } }; } }

export async function getGoogleOptions(signal?: AbortSignal) {
  return (await api.get<{ clientId: string | null; nonce: string | null }>('/auth/google/options', { signal })).data;
}

let loading: Promise<GoogleIdentityApi> | undefined;
export function loadGoogleIdentity(): Promise<GoogleIdentityApi> {
  if (window.google?.accounts.id) return Promise.resolve(window.google.accounts.id);
  if (loading) return loading;
  loading = new Promise<GoogleIdentityApi>((resolve, reject) => {
    const script = document.createElement('script');
    const timeout = setTimeout(() => { script.remove(); loading = undefined; reject(new Error('Google loading timeout')); }, 15_000);
    script.src = 'https://accounts.google.com/gsi/client';
    script.async = true;
    script.defer = true;
    script.onload = () => {
      clearTimeout(timeout);
      if (window.google?.accounts.id) resolve(window.google.accounts.id);
      else { script.remove(); loading = undefined; reject(new Error('Google unavailable')); }
    };
    script.onerror = () => { clearTimeout(timeout); script.remove(); loading = undefined; reject(new Error('Google loading failed')); };
    document.head.append(script);
  });
  return loading;
}

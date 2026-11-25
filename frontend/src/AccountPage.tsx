import { useEffect, useState, type FormEvent } from 'react'

type Account = { email: string; role: 'customer' | 'provider' }
type Session = { status: 'loading' | 'signed-out' | 'error' } | { status: 'signed-in'; account: Account }
type Notice = { status: 'info' | 'error'; text: string }

async function accountRequest(action: string, body: Record<string, string> = {}) {
  return fetch(`/api/accounts/${action}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

async function getSession(signal?: AbortSignal): Promise<Session> {
  const response = await fetch('/api/accounts/me', { signal })
  if (response.status === 401) return { status: 'signed-out' }
  if (!response.ok) return { status: 'error' }
  return { status: 'signed-in', account: await response.json() }
}

export function ConfirmEmailPage() {
  const params = new URLSearchParams(window.location.search)
  const userId = params.get('userId')
  const code = params.get('code')
  const [pending, setPending] = useState(false)
  const [notice, setNotice] = useState('')

  async function confirmEmail() {
    if (!userId || !code) return
    setPending(true)
    setNotice('')
    try {
      const response = await accountRequest('confirm-email', { userId, code })
      if (response.ok) {
        window.location.replace('/account?confirmed=1')
      } else {
        setNotice(response.status === 400
          ? 'Länken är ogiltig eller har gått ut. Du kan begära en ny länk på kontosidan.'
          : 'E-postadressen kunde inte bekräftas. Försök igen senare.')
      }
    } catch {
      setNotice('E-postadressen kunde inte bekräftas. Försök igen senare.')
    } finally {
      setPending(false)
    }
  }

  return (
    <main>
      <a href="/account">Till kontot</a>
      <h1>Bekräfta din e-postadress</h1>
      {userId && code
        ? <button type="button" disabled={pending} onClick={confirmEmail}>Bekräfta e-post</button>
        : <p role="alert">Bekräftelselänken saknas eller är ofullständig.</p>}
      {notice && <p role="alert">{notice}</p>}
    </main>
  )
}

export default function AccountPage() {
  const [session, setSession] = useState<Session>({ status: 'loading' })
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [pending, setPending] = useState(false)
  const [notice, setNotice] = useState<Notice | null>(() =>
    new URLSearchParams(window.location.search).has('confirmed')
      ? { status: 'info', text: 'Din e-postadress är bekräftad. Du kan nu logga in.' }
      : null,
  )

  useEffect(() => {
    const controller = new AbortController()
    getSession(controller.signal)
      .then((session) => {
        if (!controller.signal.aborted) setSession(session)
      })
      .catch(() => {
        if (!controller.signal.aborted) setSession({ status: 'error' })
      })
    return () => controller.abort()
  }, [])

  async function submitAccount(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const fields = new FormData(event.currentTarget)
    const action = (event.nativeEvent as SubmitEvent).submitter?.getAttribute('value') === 'resend'
      ? 'resend-confirmation'
      : mode
    const emailField = event.currentTarget.elements.namedItem('email')
    if (action === 'resend-confirmation' && emailField instanceof HTMLInputElement && !emailField.reportValidity()) {
      return
    }
    const email = String(fields.get('email') ?? '')
    const password = String(fields.get('password') ?? '')
    const role = String(fields.get('role') ?? '')

    setPending(true)
    setNotice(null)
    try {
      const response = await accountRequest(action,
        action === 'resend-confirmation' ? { email } : { email, password, role })
      if (response.status === 429) {
        setNotice({ status: 'error', text: 'För många försök. Vänta en stund och försök igen.' })
      } else if (response.ok && action === 'login') {
        const currentSession = await getSession()
        setSession(currentSession)
        if (currentSession.status === 'signed-out') {
          setNotice({ status: 'error', text: 'Inloggningen sparades inte. Försök igen.' })
        }
      } else if (response.ok) {
        setNotice({ status: 'info', text: 'Om adressen behöver bekräftas skickas en länk till den.' })
      } else if (response.status === 401) {
        setNotice({ status: 'error', text: 'Inloggningen misslyckades. Kontrollera uppgifterna och att e-postadressen är bekräftad.' })
      } else if (response.status === 400 && action === 'register') {
        setNotice({ status: 'error', text: 'Kontrollera e-postadress, lösenord och vald kontotyp.' })
      } else {
        setNotice({ status: 'error', text: 'Begäran kunde inte slutföras. Försök igen senare.' })
      }
    } catch {
      setNotice({ status: 'error', text: 'Begäran kunde inte slutföras. Försök igen senare.' })
    } finally {
      setPending(false)
    }
  }

  async function signOut() {
    setPending(true)
    setNotice(null)
    try {
      const response = await accountRequest('logout')
      if (!response.ok && response.status !== 401) throw new Error('Sign-out failed')
      setSession({ status: 'signed-out' })
    } catch {
      setNotice({ status: 'error', text: 'Utloggningen misslyckades. Försök igen.' })
    } finally {
      setPending(false)
    }
  }

  return (
    <main>
      <a href="/">Till katalogen</a>
      <h1>{session.status === 'signed-in' ? 'Mitt konto' : mode === 'login' ? 'Logga in' : 'Skapa konto'}</h1>
      {session.status === 'loading' && <p role="status">Kontrollerar inloggning…</p>}
      {session.status === 'error' && <p role="alert">Kontot kunde inte hämtas. Försök igen senare.</p>}
      {notice && <p role={notice.status === 'error' ? 'alert' : 'status'}>{notice.text}</p>}
      {session.status === 'signed-in' && (
        <>
          <p>{session.account.email} · {session.account.role === 'provider' ? 'Utförare' : 'Kund'}</p>
          {session.account.role === 'provider' && <p><a href="/account/services">Mina tjänster</a></p>}
          <button type="button" disabled={pending} onClick={signOut}>Logga ut</button>
        </>
      )}
      {session.status === 'signed-out' && (
        <>
          <form className="account-form" onSubmit={submitAccount}>
            <label htmlFor="email">E-postadress</label>
            <input id="email" name="email" type="email" autoComplete="email" required maxLength={254} />
            <label htmlFor="password">Lösenord</label>
            {mode === 'register' && <p id="password-hint">Minst 12 tecken med stor och liten bokstav, siffra och specialtecken.</p>}
            <input
              key={mode}
              id="password"
              name="password"
              type="password"
              autoComplete={mode === 'register' ? 'new-password' : 'current-password'}
              aria-describedby={mode === 'register' ? 'password-hint' : undefined}
              required
              minLength={mode === 'register' ? 12 : undefined}
              maxLength={128}
            />
            {mode === 'register' && (
              <>
                <label htmlFor="role">Kontotyp</label>
                <select id="role" name="role">
                  <option value="customer">Kund</option>
                  <option value="provider">Utförare</option>
                </select>
              </>
            )}
            <button type="submit" disabled={pending}>{mode === 'login' ? 'Logga in' : 'Skapa konto'}</button>
            {mode === 'login' && (
              <button type="submit" value="resend" formNoValidate className="secondary" disabled={pending}>
                Skicka ny bekräftelselänk
              </button>
            )}
          </form>
          <button type="button" className="secondary" disabled={pending} onClick={() => {
            setMode(mode === 'login' ? 'register' : 'login')
            setNotice(null)
          }}>
            {mode === 'login' ? 'Skapa ett konto' : 'Jag har redan ett konto'}
          </button>
        </>
      )}
    </main>
  )
}

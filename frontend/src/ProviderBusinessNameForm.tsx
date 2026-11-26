import { useState, type FormEvent } from 'react'

export default function ProviderBusinessNameForm({ initialName }: { initialName: string | null }) {
  const [name, setName] = useState(initialName ?? '')
  const [pending, setPending] = useState(false)
  const [notice, setNotice] = useState<{ error: boolean; text: string } | null>(null)

  async function saveName(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const businessName = name.trim()
    if (!businessName) {
      setNotice({ error: true, text: 'Ange ett företagsnamn.' })
      return
    }

    setPending(true)
    setNotice(null)
    try {
      const response = await fetch('/api/provider/profile', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ businessName }),
      })
      if (!response.ok) throw new Error('Business name update failed')

      const saved: { businessName: string } = await response.json()
      setName(saved.businessName)
      setNotice({ error: false, text: 'Företagsnamnet är sparat.' })
    } catch {
      setNotice({ error: true, text: 'Företagsnamnet kunde inte sparas. Försök igen.' })
    } finally {
      setPending(false)
    }
  }

  return (
    <section className="provider-profile" aria-labelledby="provider-profile-heading">
      <h2 id="provider-profile-heading">Företagsuppgifter</h2>
      <form className="account-form" onSubmit={saveName}>
        <label htmlFor="business-name">Företagsnamn</label>
        <input id="business-name" autoComplete="organization" required maxLength={120} disabled={pending}
          aria-describedby="business-name-hint" value={name} onChange={(event) => setName(event.target.value)} />
        <p id="business-name-hint">Namnet visas på dina publicerade tjänster.</p>
        <button type="submit" disabled={pending}>Spara företagsnamn</button>
        {notice && <p role={notice.error ? 'alert' : 'status'}>{notice.text}</p>}
      </form>
    </section>
  )
}

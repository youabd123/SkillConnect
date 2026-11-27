import { useState, type FormEvent } from 'react'

export default function ProviderProfileForm({ initialName, initialDescription }: {
  initialName: string | null
  initialDescription: string | null
}) {
  const [name, setName] = useState(initialName ?? '')
  const [description, setDescription] = useState(initialDescription ?? '')
  const [pending, setPending] = useState(false)
  const [notice, setNotice] = useState<{ error: boolean; text: string } | null>(null)

  async function saveProfile(event: FormEvent<HTMLFormElement>) {
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
        body: JSON.stringify({ businessName, businessDescription: description }),
      })
      if (!response.ok) throw new Error('Business name update failed')

      const saved: { businessName: string; businessDescription: string | null } = await response.json()
      setName(saved.businessName)
      setDescription(saved.businessDescription ?? '')
      setNotice({ error: false, text: 'Företagsprofilen är sparad.' })
    } catch {
      setNotice({ error: true, text: 'Företagsprofilen kunde inte sparas. Försök igen.' })
    } finally {
      setPending(false)
    }
  }

  return (
    <section className="provider-profile" aria-labelledby="provider-profile-heading">
      <h2 id="provider-profile-heading">Företagsuppgifter</h2>
      <form className="account-form" onSubmit={saveProfile}>
        <label htmlFor="business-name">Företagsnamn</label>
        <input id="business-name" autoComplete="organization" required maxLength={120} disabled={pending}
          aria-describedby="business-name-hint" value={name} onChange={(event) => setName(event.target.value)} />
        <p id="business-name-hint">Namnet visas på dina publicerade tjänster.</p>
        <label htmlFor="business-description">Kort presentation (valfritt)</label>
        <textarea id="business-description" rows={4} maxLength={1000} disabled={pending}
          value={description} onChange={(event) => setDescription(event.target.value)} />
        <button type="submit" disabled={pending}>Spara företagsprofil</button>
        {notice && <p role={notice.error ? 'alert' : 'status'}>{notice.text}</p>}
      </form>
    </section>
  )
}

import { useEffect, useState, type FormEvent } from 'react'
import { bookingTimeZone, formatBookingTime } from './bookingTimes'

type AvailableTime = { id: string; startsAt: string; endsAt: string }
type AvailabilityState =
  | { status: 'loading' | 'error' }
  | { status: 'ready'; times: AvailableTime[] }

function parseLocalTime(value: string) {
  const date = new Date(value)
  if (!Number.isFinite(date.getTime())) return null

  const pad = (part: number) => String(part).padStart(2, '0')
  const roundTrip = `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
  return roundTrip === value ? date : null
}

export default function ServiceAvailability({ serviceId, managed = false }: { serviceId: string; managed?: boolean }) {
  const url = `/api/${managed ? 'provider/' : ''}services/${encodeURIComponent(serviceId)}/availability`
  const [state, setState] = useState<AvailabilityState>({ status: 'loading' })
  const [pending, setPending] = useState(false)
  const [notice, setNotice] = useState<{ error: boolean; text: string; showAccountLink?: boolean } | null>(null)

  useEffect(() => {
    const controller = new AbortController()

    async function loadTimes() {
      try {
        const response = await fetch(url, { signal: controller.signal })
        if (!response.ok) throw new Error('Availability request failed')
        const times: AvailableTime[] = await response.json()
        if (!controller.signal.aborted) setState({ status: 'ready', times })
      } catch {
        if (!controller.signal.aborted) setState({ status: 'error' })
      }
    }

    void loadTimes()
    return () => controller.abort()
  }, [url])

  async function addTime(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = event.currentTarget
    const fields = new FormData(form)
    const startsAt = parseLocalTime(String(fields.get('startsAt') ?? ''))
    const endsAt = parseLocalTime(String(fields.get('endsAt') ?? ''))
    if (!startsAt || !endsAt || startsAt.getTime() <= Date.now() || endsAt <= startsAt) {
      setNotice({ error: true, text: 'Välj en starttid i framtiden och en sluttid efter starttiden.' })
      return
    }

    setPending(true)
    setNotice(null)
    try {
      const response = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ startsAt: startsAt.toISOString(), endsAt: endsAt.toISOString() }),
      })
      if (response.status === 400) {
        setNotice({ error: true, text: 'Välj en starttid i framtiden och en sluttid efter starttiden.' })
        return
      }
      if (!response.ok) throw new Error('Availability creation failed')

      const time: AvailableTime = await response.json()
      setState((current) => current.status === 'ready' ? {
        status: 'ready',
        times: [...current.times, time].sort((first, second) => Date.parse(first.startsAt) - Date.parse(second.startsAt)),
      } : current)
      form.reset()
      setNotice({ error: false, text: 'Tiden är tillagd.' })
    } catch {
      setNotice({ error: true, text: 'Tiden kunde inte läggas till. Försök igen.' })
    } finally {
      setPending(false)
    }
  }

  async function removeTime(timeId: string) {
    setPending(true)
    setNotice(null)
    try {
      const response = await fetch(`${url}/${encodeURIComponent(timeId)}`, { method: 'DELETE' })
      if (!response.ok && response.status !== 404) throw new Error('Availability removal failed')

      hideTime(timeId)
      setNotice({
        error: false,
        text: response.status === 404 ? 'Tiden är inte längre tillgänglig.' : 'Tiden är borttagen.',
      })
    } catch {
      setNotice({ error: true, text: 'Tiden kunde inte tas bort. Försök igen.' })
    } finally {
      setPending(false)
    }
  }

  function hideTime(timeId: string) {
    setState((current) => current.status === 'ready'
      ? { status: 'ready', times: current.times.filter((time) => time.id !== timeId) }
      : current)
  }

  async function bookTime(time: AvailableTime) {
    setPending(true)
    setNotice(null)
    try {
      const response = await fetch(`${url}/${encodeURIComponent(time.id)}/booking`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: '{}',
      })
      if (response.status === 401) {
        setNotice({ error: true, text: 'Logga in som kund för att boka en tid.', showAccountLink: true })
        return
      }
      if (response.status === 403) {
        setNotice({ error: true, text: 'Du behöver ett kundkonto för att boka en tid.' })
        return
      }
      if (response.status === 409) {
        hideTime(time.id)
        setNotice({ error: true, text: 'Tiden är inte längre ledig. Välj en annan tid om någon finns.' })
        return
      }
      if (!response.ok) throw new Error('Booking failed')

      hideTime(time.id)
      setNotice({
        error: false,
        text: `Bokningen är bekräftad. ${formatBookingTime(time.startsAt, time.endsAt)}.`,
        showAccountLink: true,
      })
    } catch {
      setNotice({ error: true, text: 'Bokningen kunde inte bekräftas. Kontrollera Mina bokningar innan du försöker igen.', showAccountLink: true })
    } finally {
      setPending(false)
    }
  }

  return (
    <section className="availability" aria-label="Lediga tider">
      {managed ? <h4>Lediga tider</h4> : <h2>Lediga tider</h2>}
      {state.status === 'loading' && <p role="status">Hämtar lediga tider…</p>}
      {state.status === 'error' && <p role="alert">Tiderna kunde inte hämtas. Försök igen senare.</p>}
      {state.status === 'ready' && (
        <>
          {notice && (
            <p role={notice.error ? 'alert' : 'status'}>
              {notice.text} {notice.showAccountLink && <a href="/account">Till kontot</a>}
            </p>
          )}
          {(managed || state.times.length > 0) && (
            <p>Tidszon: {bookingTimeZone}</p>
          )}
          {managed && (
            <form onSubmit={addTime}>
              <fieldset className="availability-form" disabled={pending}>
                <label htmlFor="availability-start">Start</label>
                <input id="availability-start" type="datetime-local" name="startsAt" required />
                <label htmlFor="availability-end">Slut</label>
                <input id="availability-end" type="datetime-local" name="endsAt" required />
                <div><button type="submit">Lägg till tid</button></div>
              </fieldset>
            </form>
          )}
          {state.times.length === 0 ? <p>Inga kommande lediga tider.</p> : (
            <ul className="availability-times">
              {state.times.map((time) => {
                const label = formatBookingTime(time.startsAt, time.endsAt)
                return (
                  <li key={time.id}>
                    <span>{label}</span>
                    {managed ? (
                      <button type="button" className="secondary" disabled={pending}
                        aria-label={`Ta bort tid ${label}`} onClick={() => void removeTime(time.id)}>
                        Ta bort
                      </button>
                    ) : (
                      <button type="button" disabled={pending} aria-label={`Boka ${label}`}
                        onClick={() => void bookTime(time)}>
                        Boka
                      </button>
                    )}
                  </li>
                )
              })}
            </ul>
          )}
        </>
      )}
    </section>
  )
}

import { useEffect, useState } from 'react'
import { bookingTimeZone, formatBookingTime } from './bookingTimes'

type Booking = {
  id: string
  serviceId: string
  serviceName: string
  startsAt: string
  endsAt: string
  isCancelled: boolean
}
type BookingView = Booking & { canCancel: boolean }
type BookingState = { status: 'loading' | 'error' } | { status: 'ready'; bookings: BookingView[] }

export default function AccountBookings({ role }: { role: 'customer' | 'provider' }) {
  const [state, setState] = useState<BookingState>({ status: 'loading' })
  const [confirmationId, setConfirmationId] = useState<string | null>(null)
  const [pending, setPending] = useState(false)
  const [notice, setNotice] = useState<{ error: boolean; text: string } | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    const url = role === 'provider' ? '/api/provider/bookings' : '/api/accounts/bookings'

    async function loadBookings() {
      try {
        const response = await fetch(url, { signal: controller.signal })
        if (!response.ok) throw new Error('Bookings request failed')
        const bookings: Booking[] = await response.json()
        const now = Date.now()
        if (!controller.signal.aborted) {
          setState({ status: 'ready', bookings: bookings.map((booking) => ({
            ...booking,
            canCancel: role === 'customer' && !booking.isCancelled && Date.parse(booking.startsAt) > now,
          })) })
        }
      } catch {
        if (!controller.signal.aborted) setState({ status: 'error' })
      }
    }

    void loadBookings()
    return () => controller.abort()
  }, [role])

  async function cancelBooking(id: string) {
    setPending(true)
    setNotice(null)
    try {
      const response = await fetch(`/api/accounts/bookings/${encodeURIComponent(id)}/cancellation`, { method: 'PUT' })
      if (response.status === 409) {
        updateCancellation(id, false)
        setConfirmationId(null)
        setNotice({ error: true, text: 'Bokningen har redan börjat och kan inte avbokas.' })
        return
      }
      if (!response.ok) throw new Error('Booking cancellation failed')

      updateCancellation(id, true)
      setConfirmationId(null)
      setNotice({ error: false, text: 'Bokningen är avbokad.' })
    } catch {
      setNotice({ error: true, text: 'Avbokningen kunde inte bekräftas. Försök igen eller ladda om sidan.' })
    } finally {
      setPending(false)
    }
  }

  function updateCancellation(id: string, isCancelled: boolean) {
    setState((current) => current.status === 'ready'
      ? { status: 'ready', bookings: current.bookings.map((booking) =>
          booking.id === id ? { ...booking, isCancelled, canCancel: false } : booking) }
      : current)
  }

  return (
    <section aria-labelledby="bookings-heading">
      <h2 id="bookings-heading">{role === 'provider' ? 'Bokningar för mina tjänster' : 'Mina bokningar'}</h2>
      {state.status === 'loading' && <p role="status">Hämtar bokningar…</p>}
      {state.status === 'error' && <p role="alert">Bokningarna kunde inte hämtas. Försök igen senare.</p>}
      {notice && <p role={notice.error ? 'alert' : 'status'}>{notice.text}</p>}
      {state.status === 'ready' && (state.bookings.length === 0 ? <p>Inga bokningar ännu.</p> : (
        <>
          <p>Tidszon: {bookingTimeZone}</p>
          <ul className="services">
            {state.bookings.map((booking) => (
              <li key={booking.id}>
                <h3><a href={`/services/${booking.serviceId}`}>{booking.serviceName}</a></h3>
                <p>{formatBookingTime(booking.startsAt, booking.endsAt)} · {booking.isCancelled ? 'Avbokad' : 'Bekräftad'}</p>
                {booking.canCancel && (
                  <div className="service-actions">
                    {confirmationId === booking.id ? (
                      <>
                        <button type="button" disabled={pending} onClick={() => void cancelBooking(booking.id)}>
                          Bekräfta avbokning
                        </button>
                        <button type="button" className="secondary" disabled={pending} onClick={() => setConfirmationId(null)}>
                          Behåll bokningen
                        </button>
                      </>
                    ) : (
                      <button type="button" className="secondary" disabled={pending} onClick={() => {
                        setNotice(null)
                        setConfirmationId(booking.id)
                      }}>
                        Avboka
                      </button>
                    )}
                  </div>
                )}
              </li>
            ))}
          </ul>
        </>
      ))}
    </section>
  )
}

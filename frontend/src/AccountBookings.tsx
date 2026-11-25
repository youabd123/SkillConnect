import { useEffect, useState } from 'react'
import { bookingTimeZone, formatBookingTime } from './bookingTimes'

type Booking = { id: string; serviceId: string; serviceName: string; startsAt: string; endsAt: string }
type BookingState = { status: 'loading' | 'error' } | { status: 'ready'; bookings: Booking[] }

export default function AccountBookings({ role }: { role: 'customer' | 'provider' }) {
  const [state, setState] = useState<BookingState>({ status: 'loading' })

  useEffect(() => {
    const controller = new AbortController()
    const url = role === 'provider' ? '/api/provider/bookings' : '/api/accounts/bookings'

    async function loadBookings() {
      try {
        const response = await fetch(url, { signal: controller.signal })
        if (!response.ok) throw new Error('Bookings request failed')
        const bookings: Booking[] = await response.json()
        if (!controller.signal.aborted) setState({ status: 'ready', bookings })
      } catch {
        if (!controller.signal.aborted) setState({ status: 'error' })
      }
    }

    void loadBookings()
    return () => controller.abort()
  }, [role])

  return (
    <section aria-labelledby="bookings-heading">
      <h2 id="bookings-heading">{role === 'provider' ? 'Bokningar för mina tjänster' : 'Mina bokningar'}</h2>
      {state.status === 'loading' && <p role="status">Hämtar bokningar…</p>}
      {state.status === 'error' && <p role="alert">Bokningarna kunde inte hämtas. Försök igen senare.</p>}
      {state.status === 'ready' && (state.bookings.length === 0 ? <p>Inga bokningar ännu.</p> : (
        <>
          <p>Tidszon: {bookingTimeZone}</p>
          <ul className="services">
            {state.bookings.map((booking) => (
              <li key={booking.id}>
                <h3><a href={`/services/${booking.serviceId}`}>{booking.serviceName}</a></h3>
                <p>{formatBookingTime(booking.startsAt, booking.endsAt)} · Bekräftad</p>
              </li>
            ))}
          </ul>
        </>
      ))}
    </section>
  )
}

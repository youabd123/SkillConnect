const localDateTime = new Intl.DateTimeFormat('sv-SE', {
  dateStyle: 'medium',
  timeStyle: 'short',
})

export const bookingTimeZone = localDateTime.resolvedOptions().timeZone

export function formatBookingTime(startsAt: string, endsAt: string) {
  return localDateTime.formatRange(new Date(startsAt), new Date(endsAt))
}

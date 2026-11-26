const localDateTime = new Intl.DateTimeFormat('sv-SE', {
  dateStyle: 'medium',
  timeStyle: 'short',
})

export const bookingTimeZone = localDateTime.resolvedOptions().timeZone

export function formatBookingStart(startsAt: string) {
  return localDateTime.format(new Date(startsAt))
}

export function formatBookingTime(startsAt: string, endsAt: string) {
  return localDateTime.formatRange(new Date(startsAt), new Date(endsAt))
}

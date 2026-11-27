export const deliveryModes = { onSite: 1, remote: 2, both: 3 } as const

export type DeliveryMode = typeof deliveryModes[keyof typeof deliveryModes]

export type ServiceDelivery = { deliveryMode: DeliveryMode | null; city: string | null }

export function describeDelivery({ deliveryMode, city }: ServiceDelivery) {
  if (deliveryMode === deliveryModes.remote) return 'På distans'
  if (deliveryMode === deliveryModes.both) return city ? `${city} · På distans` : 'På plats och på distans'
  return city
}

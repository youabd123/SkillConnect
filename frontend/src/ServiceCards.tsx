import { formatBookingStart } from './bookingTimes'
import { describeDelivery, type ServiceDelivery } from './serviceDelivery'

export type ServiceSummary = ServiceDelivery & {
  id: string
  name: string
  category: string
  nextAvailableAt: string | null
  businessName: string | null
  providerId: string | null
}

export default function ServiceCards({ services, search = '', showProvider = true }: {
  services: ServiceSummary[]
  search?: string
  showProvider?: boolean
}) {
  return (
    <ul className="catalog-grid">
      {services.map((service) => (
        <li className="catalog-card" key={service.id}>
          <p className="category-label">{service.category}</p>
          <div className="catalog-card-title">
            <h3><a className="catalog-card-link" href={`/services/${service.id}${search}`}>{service.name}</a></h3>
            {showProvider && service.businessName && (
              <p className="business-name">
                {service.providerId
                  ? <a className="card-provider-link" href={`/providers/${service.providerId}`}>{service.businessName}</a>
                  : service.businessName}
              </p>
            )}
            {describeDelivery(service) && <p className="service-location">{describeDelivery(service)}</p>}
          </div>
          {service.nextAvailableAt ? (
            <p className="card-availability">
              <span>Nästa lediga tid</span>
              <time dateTime={service.nextAvailableAt}>{formatBookingStart(service.nextAvailableAt)}</time>
            </p>
          ) : <p className="card-availability">Inga lediga tider just nu</p>}
          <span className="card-arrow" aria-hidden="true">↗</span>
        </li>
      ))}
    </ul>
  )
}

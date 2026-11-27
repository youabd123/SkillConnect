import { useEffect, useState } from 'react'
import ServiceAvailability from './ServiceAvailability'
import { describeDelivery, type ServiceDelivery } from './serviceDelivery'

type ServiceDetails = ServiceDelivery & {
  name: string
  description: string
  category: string
  businessName: string | null
  providerId: string | null
}

type DetailsResult =
  | { status: 'loading' }
  | { status: 'success'; service: ServiceDetails }
  | { status: 'missing' }
  | { status: 'error' }

export default function ServiceDetailsPage({ id, backHref }: { id: string; backHref: string }) {
  const [result, setResult] = useState<DetailsResult>({ status: 'loading' })

  useEffect(() => {
    const controller = new AbortController()

    async function loadDetails() {
      try {
        const response = await fetch(`/api/services/${encodeURIComponent(id)}`, {
          signal: controller.signal,
        })
        if (response.status === 404) {
          if (!controller.signal.aborted) {
            setResult({ status: 'missing' })
          }
          return
        }
        if (!response.ok) {
          throw new Error(`Service details returned HTTP ${response.status}`)
        }

        const service: ServiceDetails = await response.json()
        if (!controller.signal.aborted) {
          setResult({ status: 'success', service })
        }
      } catch {
        if (!controller.signal.aborted) {
          setResult({ status: 'error' })
        }
      }
    }

    void loadDetails()
    return () => controller.abort()
  }, [id])

  return (
    <main className="details-page">
      <a className="back-link" href={backHref}><span aria-hidden="true">←</span> Tillbaka till katalogen</a>
      {result.status === 'loading' && <p role="status">Hämtar tjänsten…</p>}
      {result.status === 'missing' && <h1>Tjänsten hittades inte</h1>}
      {result.status === 'error' && (
        <p role="alert">Tjänsten kunde inte hämtas. Försök igen senare.</p>
      )}
      {result.status === 'success' && (
        <article className="service-detail">
          <div className="detail-copy">
            <p className="category-label">{result.service.category}</p>
            <h1>{result.service.name}</h1>
            {result.service.businessName && (
              <p className="business-name detail-business-name">
                Erbjuds av {result.service.providerId
                  ? <a href={`/providers/${result.service.providerId}`}>{result.service.businessName}</a>
                  : result.service.businessName}
              </p>
            )}
            {describeDelivery(result.service) && <p className="service-location">{describeDelivery(result.service)}</p>}
            <p className="service-description">{result.service.description}</p>
          </div>
          <ServiceAvailability key={id} serviceId={id} />
        </article>
      )}
    </main>
  )
}

import { useEffect, useState } from 'react'
import ServiceCards, { type ServiceSummary } from './ServiceCards'

type Profile = { businessName: string; businessDescription: string | null; services: ServiceSummary[] }
type ProfileState = { status: 'loading' | 'missing' | 'error' } | { status: 'ready'; profile: Profile }

export default function PublicProviderPage({ id }: { id: string }) {
  const [state, setState] = useState<ProfileState>({ status: 'loading' })

  useEffect(() => {
    const controller = new AbortController()

    async function loadProfile() {
      try {
        const response = await fetch(`/api/providers/${encodeURIComponent(id)}`, { signal: controller.signal })
        if (controller.signal.aborted) return
        if (response.status === 404) {
          setState({ status: 'missing' })
          return
        }
        if (!response.ok) throw new Error('Provider profile request failed')
        const profile: Profile = await response.json()
        if (!controller.signal.aborted) setState({ status: 'ready', profile })
      } catch {
        if (!controller.signal.aborted) setState({ status: 'error' })
      }
    }

    void loadProfile()
    return () => controller.abort()
  }, [id])

  return (
    <main className="catalog-page public-provider-page">
      <a className="back-link" href="/"><span aria-hidden="true">←</span> Tillbaka till katalogen</a>
      {state.status === 'loading' && <p role="status">Hämtar företagsprofilen…</p>}
      {state.status === 'missing' && <h1>Företagsprofilen hittades inte</h1>}
      {state.status === 'error' && <p role="alert">Företagsprofilen kunde inte hämtas. Försök igen senare.</p>}
      {state.status === 'ready' && (
        <>
          <h1>{state.profile.businessName}</h1>
          {state.profile.businessDescription && <p className="service-description">{state.profile.businessDescription}</p>}
          <section className="catalog-section" aria-labelledby="provider-services-heading">
            <h2 id="provider-services-heading">Företagets tjänster</h2>
            {state.profile.services.length > 0
              ? <ServiceCards services={state.profile.services} showProvider={false} />
              : <p className="catalog-message">Företaget har inga publicerade tjänster ännu.</p>}
          </section>
        </>
      )}
    </main>
  )
}

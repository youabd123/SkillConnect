import { useEffect, useRef, useState, type FormEvent } from 'react'
import AccountPage, { ConfirmEmailPage } from './AccountPage'
import ProviderServicesPage from './ProviderServicesPage'
import ServiceDetailsPage from './ServiceDetailsPage'
import PublicProviderPage from './PublicProviderPage'
import ServiceCards, { type ServiceSummary } from './ServiceCards'

const categories = [
  { name: 'IT & teknik', icon: '💻', aliases: ['IT', 'Teknik', 'IT-support', 'Webbutveckling'] },
  { name: 'Frisör & skönhet', icon: '✂️', aliases: ['Frisör', 'Skönhet', 'Barberare'] },
  { name: 'Restaurang', icon: '🍽️', aliases: ['Restauranger', 'Bordbokning', 'Mat & dryck'] },
  { name: 'Hem & trädgård', icon: '🪴', aliases: ['Hemservice', 'Trädgård', 'Städning', 'Hantverk'] },
  { name: 'Foto & event', icon: '📷', aliases: ['Foto', 'Fotograf', 'Event'] },
  { name: 'Hälsa & träning', icon: '🏃', aliases: ['Hälsa', 'Träning', 'Massage'] },
  { name: 'Fordon', icon: '🚗', aliases: ['Bil', 'Bilservice', 'Bilverkstad'] },
  { name: 'Utbildning', icon: '📚', aliases: ['Kurser', 'Läxhjälp', 'Undervisning'] },
]

const categoryComparer = new Intl.Collator('sv-SE', { sensitivity: 'base' })

type CatalogFilters = { category: string; query: string; city: string; remote: boolean }
const emptyFilters: CatalogFilters = { category: '', query: '', city: '', remote: false }

function catalogSearch({ category, query, city, remote }: CatalogFilters) {
  const params = new URLSearchParams()
  if (category) params.set('category', category)
  if (query) params.set('q', query)
  if (city) params.set('city', city)
  if (remote) params.set('remote', 'true')
  return params.size ? `?${params}` : ''
}

type CatalogResult =
  | { status: 'loading' }
  | { status: 'success'; services: ServiceSummary[] }
  | { status: 'error' }

function ServiceResults({ filters }: { filters: CatalogFilters }) {
  const [result, setResult] = useState<CatalogResult>({ status: 'loading' })
  const { category, query, city, remote } = filters

  useEffect(() => {
    const controller = new AbortController()
    const group = categories.find((item) => categoryComparer.compare(item.name, category) === 0)
    const search = catalogSearch({ category: group ? '' : category, query, city, remote })

    async function loadServices() {
      try {
        const response = await fetch(`/api/services${search}`, { signal: controller.signal })
        if (!response.ok) {
          throw new Error(`Service catalog returned HTTP ${response.status}`)
        }

        const services: ServiceSummary[] = await response.json()
        const matches = group ? services.filter((service) =>
          [group.name, ...group.aliases].some((name) => categoryComparer.compare(name, service.category) === 0)) : services
        if (!controller.signal.aborted) {
          setResult({ status: 'success', services: matches })
        }
      } catch {
        if (!controller.signal.aborted) {
          setResult({ status: 'error' })
        }
      }
    }

    void loadServices()
    return () => controller.abort()
  }, [category, query, city, remote])

  if (result.status === 'loading') {
    return <p className="catalog-message" role="status">Hämtar tjänster…</p>
  }

  if (result.status === 'error') {
    return <p className="catalog-message" role="alert">Tjänsterna kunde inte hämtas. Försök igen senare.</p>
  }

  if (result.services.length === 0) {
    const emptyMessage = category || query || city || remote
      ? 'Inga tjänster matchar din sökning.' : 'Inga publicerade tjänster ännu.'
    return <p className="catalog-message" role="status">{emptyMessage}</p>
  }

  return <ServiceCards services={result.services} search={catalogSearch(filters)} />
}

function ServiceCatalog({ initialFilters }: { initialFilters: CatalogFilters }) {
  const [draft, setDraft] = useState(initialFilters)
  const [filters, setFilters] = useState(initialFilters)
  const categoryRow = useRef<HTMLUListElement>(null)

  function applyFilters(input: CatalogFilters) {
    const next = { ...input, category: input.category.trim(), query: input.query.trim(), city: input.city.trim() }
    setDraft(next)
    window.history.replaceState(null, '', `/${catalogSearch(next)}${window.location.hash}`)
    setFilters(next)
  }

  function applyCategory(value: string) {
    applyFilters({ ...draft, category: value })
  }

  function filterServices(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    applyFilters(draft)
  }

  return (
    <main className="catalog-page">
      <nav className="category-nav" aria-label="Tjänstekategorier">
        <ul className="category-row" ref={categoryRow}>
          <li>
            <button type="button" className="category-tab" aria-pressed={!filters.category} onClick={() => applyCategory('')}>
              <span className="category-icon category-icon-all" aria-hidden="true">⊞</span>
              Alla
            </button>
          </li>
          {categories.map((item) => (
            <li key={item.name}>
              <button type="button" className="category-tab"
                aria-pressed={categoryComparer.compare(filters.category, item.name) === 0}
                onClick={() => applyCategory(item.name)}>
                <span className="category-icon" aria-hidden="true">{item.icon}</span>
                {item.name}
              </button>
            </li>
          ))}
        </ul>
        <div className="category-arrows">
          <button type="button" className="secondary" aria-label="Bläddra kategorier åt vänster"
            onClick={() => categoryRow.current?.scrollBy({ left: -320 })}>‹</button>
          <button type="button" className="secondary" aria-label="Bläddra kategorier åt höger"
            onClick={() => categoryRow.current?.scrollBy({ left: 320 })}>›</button>
        </div>
      </nav>
      <section className="catalog-hero" aria-labelledby="catalog-title">
        <div className="hero-copy">
          <h1 id="catalog-title">Hitta och boka tjänster.</h1>
          <p>Utforska tjänster från företag i olika branscher och boka en tid som passar dig.</p>
          <a className="hero-link" href="#tjanster">Utforska tjänster <span aria-hidden="true">↓</span></a>
        </div>
        <div className="hero-art" aria-hidden="true" />
      </section>
      <section className="catalog-section" id="tjanster" aria-labelledby="services-title">
        <div className="catalog-toolbar">
          <h2 id="services-title">{filters.category || 'Alla tjänster'}</h2>
          <form className="catalog-filter" onSubmit={filterServices} role="search" aria-label="Sök i tjänstekatalogen">
            <div className="filter">
              <div className="catalog-search-field">
                <label htmlFor="catalog-search">Tjänst eller företag</label>
                <input id="catalog-search" type="search" value={draft.query} maxLength={120}
                  placeholder="Till exempel Wi-Fi" onChange={(event) => setDraft({ ...draft, query: event.target.value })} />
              </div>
              <div>
                <label htmlFor="category">Kategori</label>
                <input id="category" value={draft.category} onChange={(event) => setDraft({ ...draft, category: event.target.value })} />
              </div>
              <div>
                <label htmlFor="catalog-city">Ort</label>
                <input id="catalog-city" value={draft.city} maxLength={100}
                  placeholder="Till exempel Göteborg" onChange={(event) => setDraft({ ...draft, city: event.target.value })} />
              </div>
              <label className="catalog-remote">
                <input type="checkbox" checked={draft.remote} onChange={(event) => setDraft({ ...draft, remote: event.target.checked })} />
                Kan bokas på distans
              </label>
              <button type="submit">Sök</button>
              {catalogSearch(filters) && <button type="button" className="secondary" onClick={() => applyFilters(emptyFilters)}>Rensa filter</button>}
            </div>
          </form>
        </div>
        <ServiceResults key={catalogSearch(filters)} filters={filters} />
      </section>
    </main>
  )
}

function CurrentPage() {
  if (window.location.pathname === '/account') return <AccountPage />
  if (window.location.pathname === '/account/services') return <ProviderServicesPage />
  if (window.location.pathname === '/confirm-email') return <ConfirmEmailPage />
  const providerId = /^\/providers\/([^/]+)\/?$/.exec(window.location.pathname)?.[1]
  if (providerId) return <PublicProviderPage id={providerId} />

  const params = new URLSearchParams(window.location.search)
  const filters: CatalogFilters = {
    category: params.get('category')?.trim() ?? '',
    query: params.get('q')?.trim().slice(0, 120) ?? '',
    city: params.get('city')?.trim().slice(0, 100) ?? '',
    remote: params.get('remote') === 'true',
  }
  const search = catalogSearch(filters)
  const serviceId = /^\/services\/([^/]+)\/?$/.exec(window.location.pathname)?.[1]

  return serviceId
    ? <ServiceDetailsPage id={serviceId} backHref={search ? `/${search}#tjanster` : '/'} />
    : <ServiceCatalog initialFilters={filters} />
}

function App() {
  return (
    <>
      <header className="site-header">
        <nav className="site-nav" aria-label="Huvudmeny">
          <a className="wordmark" href="/">
            <span className="brand-mark" aria-hidden="true">S</span>
            SkillConnect
          </a>
          <div className="main-links">
            <a href="/">Utforska</a>
            <a href="/account/services">Erbjud en tjänst</a>
          </div>
          <a className="account-link" href="/account">Mitt konto <span aria-hidden="true">↗</span></a>
        </nav>
      </header>
      <CurrentPage />
    </>
  )
}

export default App

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

function catalogSearch(category: string, query: string) {
  const params = new URLSearchParams()
  if (category) params.set('category', category)
  if (query) params.set('q', query)
  return params.size ? `?${params}` : ''
}

type CatalogResult =
  | { status: 'loading' }
  | { status: 'success'; services: ServiceSummary[] }
  | { status: 'error' }

function ServiceResults({ category, query }: { category: string; query: string }) {
  const [result, setResult] = useState<CatalogResult>({ status: 'loading' })

  useEffect(() => {
    const controller = new AbortController()
    const group = categories.find((item) => categoryComparer.compare(item.name, category) === 0)
    const search = catalogSearch(group ? '' : category, query)

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
  }, [category, query])

  if (result.status === 'loading') {
    return <p className="catalog-message" role="status">Hämtar tjänster…</p>
  }

  if (result.status === 'error') {
    return <p className="catalog-message" role="alert">Tjänsterna kunde inte hämtas. Försök igen senare.</p>
  }

  if (result.services.length === 0) {
    const emptyMessage = query ? `Inga tjänster matchar ”${query}”.`
      : category ? 'Inga tjänster i den kategorin.' : 'Inga publicerade tjänster ännu.'
    return <p className="catalog-message" role="status">{emptyMessage}</p>
  }

  return <ServiceCards services={result.services} search={catalogSearch(category, query)} />
}

function ServiceCatalog({ initialCategory, initialQuery }: { initialCategory: string; initialQuery: string }) {
  const [categoryInput, setCategoryInput] = useState(initialCategory)
  const [category, setCategory] = useState(initialCategory)
  const [searchInput, setSearchInput] = useState(initialQuery)
  const [query, setQuery] = useState(initialQuery)
  const categoryRow = useRef<HTMLUListElement>(null)

  function applyFilters(categoryValue: string, searchValue: string) {
    const category = categoryValue.trim()
    const query = searchValue.trim()
    setCategoryInput(category)
    setSearchInput(query)
    window.history.replaceState(null, '', `/${catalogSearch(category, query)}${window.location.hash}`)
    setCategory(category)
    setQuery(query)
  }

  function applyCategory(value: string) {
    applyFilters(value, searchInput)
  }

  function filterServices(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    applyFilters(categoryInput, searchInput)
  }

  return (
    <main className="catalog-page">
      <nav className="category-nav" aria-label="Tjänstekategorier">
        <ul className="category-row" ref={categoryRow}>
          <li>
            <button type="button" className="category-tab" aria-pressed={!category} onClick={() => applyCategory('')}>
              <span className="category-icon category-icon-all" aria-hidden="true">⊞</span>
              Alla
            </button>
          </li>
          {categories.map((item) => (
            <li key={item.name}>
              <button type="button" className="category-tab"
                aria-pressed={categoryComparer.compare(category, item.name) === 0}
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
          <h2 id="services-title">{category || 'Alla tjänster'}</h2>
          <form className="catalog-filter" onSubmit={filterServices} role="search" aria-label="Sök i tjänstekatalogen">
            <div className="filter">
              <div className="catalog-search-field">
                <label htmlFor="catalog-search">Tjänst eller företag</label>
                <input id="catalog-search" type="search" value={searchInput} maxLength={120}
                  placeholder="Till exempel Wi-Fi" onChange={(event) => setSearchInput(event.target.value)} />
              </div>
              <div>
                <label htmlFor="category">Kategori</label>
                <input id="category" value={categoryInput} onChange={(event) => setCategoryInput(event.target.value)} />
              </div>
              <button type="submit">Sök</button>
              {query && <button type="button" className="secondary" onClick={() => applyFilters(category, '')}>Rensa sökning</button>}
              {category && <button type="button" className="secondary" onClick={() => applyCategory('')}>Alla kategorier</button>}
            </div>
          </form>
        </div>
        <ServiceResults key={catalogSearch(category, query)} category={category} query={query} />
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
  const category = params.get('category')?.trim() ?? ''
  const query = params.get('q')?.trim().slice(0, 120) ?? ''
  const search = catalogSearch(category, query)
  const serviceId = /^\/services\/([^/]+)\/?$/.exec(window.location.pathname)?.[1]

  return serviceId
    ? <ServiceDetailsPage id={serviceId} backHref={search ? `/${search}#tjanster` : '/'} />
    : <ServiceCatalog initialCategory={category} initialQuery={query} />
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

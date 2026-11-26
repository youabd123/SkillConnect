import { useEffect, useRef, useState, type FormEvent } from 'react'
import AccountPage, { ConfirmEmailPage } from './AccountPage'
import ProviderServicesPage from './ProviderServicesPage'
import ServiceDetailsPage from './ServiceDetailsPage'
import { formatBookingStart } from './bookingTimes'

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

function categorySearch(category: string) {
  return category ? `?${new URLSearchParams({ category })}` : ''
}

type ServiceSummary = {
  id: string
  name: string
  category: string
  nextAvailableAt: string | null
  businessName: string | null
}

type CatalogResult =
  | { status: 'loading' }
  | { status: 'success'; services: ServiceSummary[] }
  | { status: 'error' }

function ServiceResults({ category }: { category: string }) {
  const [result, setResult] = useState<CatalogResult>({ status: 'loading' })

  useEffect(() => {
    const controller = new AbortController()
    const group = categories.find((item) => categoryComparer.compare(item.name, category) === 0)
    const search = group ? '' : categorySearch(category)

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
  }, [category])

  if (result.status === 'loading') {
    return <p className="catalog-message" role="status">Hämtar tjänster…</p>
  }

  if (result.status === 'error') {
    return <p className="catalog-message" role="alert">Tjänsterna kunde inte hämtas. Försök igen senare.</p>
  }

  if (result.services.length === 0) {
    return <p className="catalog-message" role="status">{category ? 'Inga tjänster i den kategorin.' : 'Inga publicerade tjänster ännu.'}</p>
  }

  return (
    <ul className="catalog-grid">
      {result.services.map((service) => (
        <li key={service.id}>
          <a className="catalog-card" href={`/services/${service.id}${categorySearch(category)}`}>
            <p className="category-label">{service.category}</p>
            <div className="catalog-card-title">
              <h3>{service.name}</h3>
              {service.businessName && <p className="business-name">{service.businessName}</p>}
            </div>
            {service.nextAvailableAt ? (
              <p className="card-availability">
                <span>Nästa lediga tid</span>
                <time dateTime={service.nextAvailableAt}>{formatBookingStart(service.nextAvailableAt)}</time>
              </p>
            ) : <p className="card-availability">Inga lediga tider just nu</p>}
            <span className="card-arrow" aria-hidden="true">↗</span>
          </a>
        </li>
      ))}
    </ul>
  )
}

function ServiceCatalog({ initialCategory }: { initialCategory: string }) {
  const [categoryInput, setCategoryInput] = useState(initialCategory)
  const [category, setCategory] = useState(initialCategory)
  const categoryRow = useRef<HTMLUListElement>(null)

  function applyCategory(value: string) {
    const category = value.trim()
    setCategoryInput(category)
    window.history.replaceState(null, '', `/${categorySearch(category)}`)
    setCategory(category)
  }

  function filterServices(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    applyCategory(categoryInput)
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
          <form onSubmit={filterServices}>
            <label htmlFor="category">Kategori</label>
            <div className="filter">
              <input id="category" value={categoryInput} onChange={(event) => setCategoryInput(event.target.value)} />
              <button type="submit">Filtrera</button>
              {category && <button type="button" className="secondary" onClick={() => applyCategory('')}>Visa alla</button>}
            </div>
          </form>
        </div>
        <ServiceResults key={category} category={category} />
      </section>
    </main>
  )
}

function CurrentPage() {
  if (window.location.pathname === '/account') return <AccountPage />
  if (window.location.pathname === '/account/services') return <ProviderServicesPage />
  if (window.location.pathname === '/confirm-email') return <ConfirmEmailPage />

  const category = new URLSearchParams(window.location.search).get('category')?.trim() ?? ''
  const serviceId = /^\/services\/([^/]+)\/?$/.exec(window.location.pathname)?.[1]

  return serviceId
    ? <ServiceDetailsPage id={serviceId} backHref={`/${categorySearch(category)}`} />
    : <ServiceCatalog initialCategory={category} />
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

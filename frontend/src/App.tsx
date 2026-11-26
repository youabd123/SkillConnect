import { useEffect, useState, type FormEvent } from 'react'
import AccountPage, { ConfirmEmailPage } from './AccountPage'
import ProviderServicesPage from './ProviderServicesPage'
import ServiceDetailsPage from './ServiceDetailsPage'

function categorySearch(category: string) {
  return category ? `?${new URLSearchParams({ category })}` : ''
}

type ServiceSummary = {
  id: string
  name: string
  category: string
}

type CatalogResult =
  | { status: 'loading' }
  | { status: 'success'; services: ServiceSummary[] }
  | { status: 'error' }

function ServiceResults({ category }: { category: string }) {
  const [result, setResult] = useState<CatalogResult>({ status: 'loading' })

  useEffect(() => {
    const controller = new AbortController()
    const search = categorySearch(category)

    async function loadServices() {
      try {
        const response = await fetch(`/api/services${search}`, { signal: controller.signal })
        if (!response.ok) {
          throw new Error(`Service catalog returned HTTP ${response.status}`)
        }

        const services: ServiceSummary[] = await response.json()
        if (!controller.signal.aborted) {
          setResult({ status: 'success', services })
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
            <h3>{service.name}</h3>
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

  function applyCategory(value: string) {
    const category = value.trim()
    window.history.replaceState(null, '', `/${categorySearch(category)}`)
    setCategory(category)
  }

  function filterServices(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    applyCategory(categoryInput)
  }

  return (
    <main className="catalog-page">
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
              {category && (
                <button type="button" className="secondary" onClick={() => {
                  setCategoryInput('')
                  applyCategory('')
                }}>Visa alla</button>
              )}
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
          <a className="account-link" href="/account">Mitt konto <span aria-hidden="true">↗</span></a>
        </nav>
      </header>
      <CurrentPage />
    </>
  )
}

export default App

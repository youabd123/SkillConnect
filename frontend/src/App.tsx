import { useEffect, useState, type FormEvent } from 'react'

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
    const search = category ? `?${new URLSearchParams({ category })}` : ''

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
    return <p role="status">Hämtar tjänster…</p>
  }

  if (result.status === 'error') {
    return <p role="alert">Tjänsterna kunde inte hämtas. Försök igen senare.</p>
  }

  if (result.services.length === 0) {
    return <p role="status">{category ? 'Inga tjänster i den kategorin.' : 'Inga publicerade tjänster ännu.'}</p>
  }

  return (
    <ul className="services">
      {result.services.map((service) => (
        <li key={service.id}>
          <h2>{service.name}</h2>
          <p>{service.category}</p>
        </li>
      ))}
    </ul>
  )
}

function App() {
  const [categoryInput, setCategoryInput] = useState('')
  const [category, setCategory] = useState('')

  function filterServices(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setCategory(categoryInput.trim())
  }

  return (
    <main>
      <h1>SkillConnect</h1>
      <form onSubmit={filterServices}>
        <label htmlFor="category">Kategori</label>
        <div className="filter">
          <input
            id="category"
            value={categoryInput}
            onChange={(event) => setCategoryInput(event.target.value)}
          />
          <button type="submit">Filtrera</button>
          {category && (
            <button type="button" className="secondary" onClick={() => {
              setCategoryInput('')
              setCategory('')
            }}>
              Visa alla
            </button>
          )}
        </div>
      </form>
      <ServiceResults key={category} category={category} />
    </main>
  )
}

export default App

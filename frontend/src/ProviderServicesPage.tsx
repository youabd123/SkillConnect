import { useEffect, useState, type FormEvent } from 'react'

type Service = {
  id: string
  name: string
  description: string
  category: string
  isPublished: boolean
}

type PageState =
  | { status: 'loading' | 'sign-in' | 'forbidden' | 'error' }
  | { status: 'ready'; services: Service[] }

const servicesUrl = '/api/provider/services'

export default function ProviderServicesPage() {
  const [page, setPage] = useState<PageState>({ status: 'loading' })
  const [editing, setEditing] = useState<Service | null>(null)
  const [pending, setPending] = useState(false)
  const [notice, setNotice] = useState<{ error: boolean; text: string } | null>(null)

  useEffect(() => {
    const controller = new AbortController()

    async function loadServices() {
      try {
        const response = await fetch(servicesUrl, { signal: controller.signal })
        if (response.ok) {
          setPage({ status: 'ready', services: await response.json() })
        } else if (response.status === 401) {
          setPage({ status: 'sign-in' })
        } else if (response.status === 403) {
          setPage({ status: 'forbidden' })
        } else {
          setPage({ status: 'error' })
        }
      } catch {
        if (!controller.signal.aborted) setPage({ status: 'error' })
      }
    }

    void loadServices()
    return () => controller.abort()
  }, [])

  async function saveService(url: string, request: RequestInit, successMessage: string) {
    setPending(true)
    setNotice(null)
    try {
      const response = await fetch(url, request)
      if (response.status === 401 || response.status === 403) {
        setPage({ status: response.status === 401 ? 'sign-in' : 'forbidden' })
        return false
      }
      if (response.status === 400) {
        setNotice({ error: true, text: 'Fyll i giltigt namn, kategori och beskrivning.' })
        return false
      }
      if (!response.ok) throw new Error(`Service request returned HTTP ${response.status}`)

      const service: Service = await response.json()
      setPage((current) => {
        if (current.status !== 'ready') return current
        const services = [...current.services.filter((item) => item.id !== service.id), service]
        services.sort((first, second) => first.name.localeCompare(second.name, 'sv'))
        return { status: 'ready', services }
      })
      setNotice({ error: false, text: successMessage })
      return true
    } catch {
      setNotice({ error: true, text: 'Ändringen kunde inte sparas. Försök igen.' })
      return false
    } finally {
      setPending(false)
    }
  }

  async function submitService(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = event.currentTarget
    const fields = new FormData(form)
    const input = {
      name: String(fields.get('name') ?? '').trim(),
      description: String(fields.get('description') ?? '').trim(),
      category: String(fields.get('category') ?? '').trim(),
    }
    const saved = await saveService(editing ? `${servicesUrl}/${editing.id}` : servicesUrl, {
      method: editing ? 'PUT' : 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(input),
    }, editing ? 'Ändringarna är sparade.' : 'Utkastet är skapat.')

    if (saved) {
      setEditing(null)
      form.reset()
    }
  }

  async function publishService(service: Service) {
    const saved = await saveService(`${servicesUrl}/${service.id}/publication`, { method: 'PUT' }, 'Tjänsten är publicerad.')
    if (saved && editing?.id === service.id) setEditing(null)
  }

  return (
    <main>
      <a href="/account">Till kontot</a>
      <h1>Mina tjänster</h1>
      {page.status === 'loading' && <p role="status">Hämtar tjänster…</p>}
      {page.status === 'sign-in' && <p role="status">Logga in för att hantera dina tjänster.</p>}
      {page.status === 'forbidden' && <p role="alert">Du behöver ett utförarkonto för att hantera tjänster.</p>}
      {page.status === 'error' && <p role="alert">Tjänsterna kunde inte hämtas. Försök igen senare.</p>}
      {page.status === 'ready' && (
        <>
          <h2 id="service-form-title">{editing ? 'Redigera tjänst' : 'Skapa tjänst'}</h2>
          {notice && <p role={notice.error ? 'alert' : 'status'}>{notice.text}</p>}
          <form key={editing?.id ?? 'new'} aria-labelledby="service-form-title" onSubmit={submitService}>
            <fieldset className="service-form" disabled={pending}>
              <label htmlFor="service-name">Namn</label>
              <input id="service-name" name="name" defaultValue={editing?.name} required maxLength={120} />
              <label htmlFor="service-category">Kategori</label>
              <input id="service-category" name="category" defaultValue={editing?.category} required maxLength={80} />
              <label htmlFor="service-description">Beskrivning</label>
              <textarea id="service-description" name="description" defaultValue={editing?.description} rows={5} required maxLength={2000} />
              <div className="service-actions">
                <button type="submit">{editing ? 'Spara ändringar' : 'Skapa utkast'}</button>
                {editing && <button type="button" className="secondary" onClick={() => setEditing(null)}>Avbryt redigering</button>}
              </div>
            </fieldset>
          </form>
          <h2>Dina tjänster</h2>
          {page.services.length === 0 ? <p>Du har inga tjänster ännu.</p> : (
            <ul className="services">
              {page.services.map((service) => (
                <li key={service.id}>
                  <h3>{service.name}</h3>
                  <p>{service.category} · {service.isPublished ? 'Publicerad' : 'Utkast'}</p>
                  <div className="service-actions">
                    <button type="button" className="secondary" disabled={pending} onClick={() => {
                      setNotice(null)
                      setEditing(service)
                    }}>Redigera</button>
                    {service.isPublished
                      ? <a href={`/services/${service.id}`}>Visa i katalogen</a>
                      : <button type="button" disabled={pending || editing?.id === service.id} onClick={() => void publishService(service)}>Publicera</button>}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </main>
  )
}

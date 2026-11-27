export const categories = [
  { key: 'tech', name: 'IT & teknik', icon: '💻', aliases: ['IT', 'Teknik', 'IT-support', 'Webbutveckling'] },
  { key: 'beauty', name: 'Frisör & skönhet', icon: '✂️', aliases: ['Frisör', 'Skönhet', 'Barberare'] },
  { key: 'food', name: 'Restaurang', icon: '🍽️', aliases: ['Restauranger', 'Bordbokning', 'Mat & dryck'] },
  { key: 'home', name: 'Hem & trädgård', icon: '🪴', aliases: ['Hemservice', 'Trädgård', 'Städning', 'Hantverk'] },
  { key: 'event', name: 'Foto & event', icon: '📷', aliases: ['Foto', 'Fotograf', 'Event'] },
  { key: 'health', name: 'Hälsa & träning', icon: '🏃', aliases: ['Hälsa', 'Träning', 'Massage'] },
  { key: 'vehicle', name: 'Fordon', icon: '🚗', aliases: ['Bil', 'Bilservice', 'Bilverkstad'] },
  { key: 'learning', name: 'Utbildning', icon: '📚', aliases: ['Kurser', 'Läxhjälp', 'Undervisning'] },
]

export const categoryComparer = new Intl.Collator('sv-SE', { sensitivity: 'base' })

export function findCatalogCategory(name: string) {
  return categories.find((category) =>
    [category.name, ...category.aliases].some((label) => categoryComparer.compare(label, name) === 0))
}

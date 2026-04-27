export default function PortfolioPanel({ portfolio }) {
  return (
    <section className="portfolio-panel">
      <div className="hero-band">
        <p className="eyebrow">Backend-focused portfolio</p>
        <h1>{portfolio.name}</h1>
        <h2>{portfolio.role}</h2>
        <p className="hero-summary">{portfolio.summary}</p>
      </div>

      <div className="content-grid">
        <article className="info-card">
          <h3>Technology focus</h3>
          <ul>
            {portfolio.technologies.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
        </article>

        <article className="info-card">
          <h3>What this portfolio demonstrates</h3>
          <ul>
            {portfolio.highlights.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
        </article>

        <article className="info-card contact-card">
          <h3>Contact</h3>
          <p>{portfolio.contact.email}</p>
          <p>
            <a href={portfolio.contact.gitHub} target="_blank" rel="noreferrer">
              GitHub
            </a>{' '}
            ·{' '}
            <a href={portfolio.contact.linkedIn} target="_blank" rel="noreferrer">
              LinkedIn
            </a>
          </p>
        </article>
      </div>
    </section>
  )
}
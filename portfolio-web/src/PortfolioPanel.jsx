import { useState, useEffect, useRef } from 'react'

/* ── Scroll reveal hook ──────────────────────────────────────── */
function useReveal(rootMargin = '-40px') {
  const ref = useRef(null)
  const [visible, setVisible] = useState(false)
  useEffect(() => {
    const el = ref.current
    if (!el) return
    const io = new IntersectionObserver(
      ([e]) => { if (e.isIntersecting) { setVisible(true); io.disconnect() } },
      { rootMargin }
    )
    io.observe(el)
    return () => io.disconnect()
  }, [rootMargin])
  return [ref, visible]
}

/* ── Count-up hook ───────────────────────────────────────────── */
function useCountUp(target, active, duration = 1000) {
  const [val, setVal] = useState(0)
  useEffect(() => {
    if (!active) return
    let cur = 0
    const steps = 40
    const inc = target / steps
    const interval = duration / steps
    const t = setInterval(() => {
      cur += inc
      if (cur >= target) { setVal(target); clearInterval(t) }
      else setVal(Math.floor(cur))
    }, interval)
    return () => clearInterval(t)
  }, [active, target, duration])
  return val
}

/* ── Architecture diagram ────────────────────────────────────── */
function ArchDiagram({ layers }) {
  return (
    <div className="arch-diagram">
      {layers.map((layer, li) => (
        <div key={li} className="arch-layer">
          {li > 0 && <div className="arch-arrow">→</div>}
          <div className="arch-nodes">
            {layer.map(node => (
              <div key={node.label} className={`arch-node${node.accent ? ' accent' : ''}`}>
                <span className="arch-node-label">{node.label}</span>
                {node.sub && <span className="arch-node-sub">{node.sub}</span>}
              </div>
            ))}
          </div>
        </div>
      ))}
    </div>
  )
}

/* ── Data ────────────────────────────────────────────────────── */
const EDUCATION = [
  {
    school: 'University of Rennes 1',
    location: 'Rennes, France',
    degree: 'Bachelor of Computer Science',
    period: 'Sep 2016 – Jun 2020',
  },
]

const EXPERIENCE = [
  {
    company: 'Capgemini',
    location: 'Ho Chi Minh City, Vietnam',
    role: 'Consultant',
    period: 'Oct 2024 – Present',
    award: 'Outstanding Newcomer',
    bullets: [
      'Modernising a large-scale French insurance platform from .NET Framework + PHP to .NET Core + React.',
      'Integrated with parent company data infrastructure via Salesforce and Guidewire.',
      'Prepared detailed technical specifications and estimations for the development team.',
      'Worked directly with product stakeholders in France to clarify and evaluate new features.',
    ],
    projects: [
      {
        year: 'Oct 2024 – Present',
        name: 'Insurance Platform Modernisation — France',
        desc: 'Modernising a large-scale insurance platform from .NET Framework + PHP to .NET Core + React. Integrated with parent company infrastructure via Salesforce (customer data) and Guidewire (policy workflows). Responsible for feature analysis, technical specs, and estimations for the dev team in France.',
        tech: ['.NET Core', 'React', 'PHP', 'Salesforce', 'Guidewire', 'SQL Server'],
      },
    ],
  },
  {
    company: 'FPT Software',
    location: 'Hanoi, Vietnam',
    role: 'Software Developer',
    period: 'Apr 2021 – Oct 2024',
    award: 'Best Performance 2021',
    bullets: [
      'Delivered across 4 enterprise projects for clients in Australia, Hong Kong, and Singapore.',
      'Led Monolith → Microservices migration on .NET Core with Domain-Driven Design.',
      'Improved API response time by up to 60% through query optimisation, schema updates, and caching.',
      'Trained ~50 intern students on software development processes and best practices.',
    ],
    projects: [
      {
        year: 'May 2021 – Oct 2024',
        name: 'ERP Software Application — Australia',
        desc: 'Business & project management platform for AEC industry — 50K+ active users worldwide. Led Monolith → Microservices migration with Domain-Driven Design. Improved API response time by 60% via query optimisation and caching. BI analytical features adopted by 100% of top 5 partners.',
        tech: ['.NET Core', 'Angular', 'SQL Server', 'Azure Service Bus', 'Cognitive Search', 'MsTest', 'NUnit'],
      },
      {
        year: 'Feb 2022 – Apr 2024',
        name: 'Insurance Mobile App — Hong Kong',
        desc: 'High-traffic mobile app (2M+ active users) offering health, entertainment and lifestyle features. Designed API and database schema for new serverless features. Achieved 40% faster real-time data retrieval via optimised caching and keyset pagination.',
        tech: ['.NET Core 6', 'AWS Lambda', 'EventBridge', 'ElasticCache', 'S3', 'DynamoDB', 'xUnit', 'Moq'],
      },
      {
        year: 'Apr 2021 – Oct 2021',
        name: 'Intelligent Audit Web App — Singapore',
        desc: 'Internal web app for Big 4 audit firm — engagement tracking and customer progress management. Led a team of 3 junior developers to deliver a Digital Staff Visiting Card with QR feature. Enforced code quality standards with SonarQ.',
        tech: ['.NET Core 5', 'Entity Framework', 'Dapper', 'SQL Server', 'xUnit', 'Moq', 'SonarQ'],
      },
    ],
  },
]

const AGENTIC_PLATFORM = {
  title: 'Agentic Platform',
  period: '2024 – Present',
  tagline: 'Three interconnected services — each independently deployable, all working as one platform.',
  repos: [
    { label: 'Portfolio', url: 'https://github.com/thanhtungngn/Porfolio' },
    { label: 'PM Agent', url: 'https://github.com/thanhtungngn/PM_Agent' },
    { label: 'PM MCP', url: 'https://github.com/thanhtungngn/PM_MCP' },
  ],
  services: [
    {
      name: 'Portfolio',
      role: 'AI-powered portfolio site',
      description: 'React 19 + ASP.NET Core with RAG chat, semantic search over ingested docs, and MCP tool-calling via OpenAI.',
      tech: ['React 19', 'ASP.NET Core', 'Qdrant', 'OpenAI', '.NET Aspire'],
      arch: [
        [{ label: 'UI', sub: 'React 19 · SPA' }],
        [{ label: 'API', sub: 'ASP.NET Core' }],
        [
          { label: 'Chat', sub: 'Minimal API' },
          { label: 'Ingest', sub: 'Minimal API' },
        ],
        [
          { label: 'OpenAI', sub: 'gpt-4o-mini', accent: true },
          { label: 'Qdrant', sub: 'Vector DB', accent: true },
          { label: 'PM_MCP', sub: 'Tool calls', accent: true },
        ],
      ],
    },
    {
      name: 'PM Agent',
      role: 'Multi-role planning agent',
      description: 'Converts a project brief or CV into a structured delivery plan or hiring recommendation using LLM role-based contributors.',
      tech: ['C#', 'LLM', 'Multi-Agent', 'Hiring Pipeline'],
      arch: [
        [{ label: 'Input', sub: 'Brief / CV' }],
        [{ label: 'Orchestrator', sub: 'C# · Role-based' }],
        [{ label: 'Planner', sub: 'OpenAI · Reasoning', accent: true }],
        [
          { label: 'Delivery Plan', sub: 'Sprint · Risk · Team' },
          { label: 'Hiring Report', sub: 'CV scoring · Fit' },
        ],
      ],
    },
    {
      name: 'PM MCP',
      role: 'Tool integration layer',
      description: 'MCP server exposing Jira, Confluence, and Trello as callable tools. Acts as the shared integration backbone for both agents.',
      tech: ['MCP', 'Node.js', 'Jira API', 'Confluence API'],
      arch: [
        [
          { label: 'Portfolio', sub: 'MCP client' },
          { label: 'PM Agent', sub: 'MCP client' },
        ],
        [{ label: 'MCP Server', sub: 'Node.js · Render', accent: true }],
        [
          { label: 'Jira', sub: 'Issues' },
          { label: 'Confluence', sub: 'Pages' },
          { label: 'Trello', sub: 'Boards' },
          { label: 'GitHub', sub: 'Repos' },
        ],
      ],
    },
  ],
}

const OTHER_PROJECTS = [
  {
    name: 'Badminton Club Management',
    period: '2026 – Present',
    description: 'Full-stack club management app for organising badminton sessions, tracking members, and managing court bookings. Built with a React frontend and .NET Core API.',
    tech: ['ASP.NET Core', 'React', 'SQL Server', 'JWT Auth'],
    github: 'https://github.com/thanhtungngn/Badminton_BE',
    liveUrl: 'https://badminton-web-lqny.onrender.com',
    inviteCode: 'BMT-X9-PRO26',
    arch: [
      [{ label: 'UI', sub: 'React · SPA' }],
      [{ label: 'REST API', sub: 'ASP.NET Core · JWT', accent: true }],
      [
        { label: 'SQL Server', sub: 'Members · Courts' },
        { label: 'Auth', sub: 'JWT · Sessions' },
      ],
    ],
  },
]

const SKILLS = [
  { group: 'Languages', items: ['C#', 'JavaScript', 'TypeScript'] },
  { group: 'Backend', items: ['.NET Core', 'ASP.NET', 'Entity Framework', '.NET Framework'] },
  { group: 'Frontend', items: ['React', 'Angular', 'AngularJS'] },
  { group: 'Database', items: ['SQL Server', 'MySQL', 'Qdrant'] },
  { group: 'Azure', items: ['Blob Storage', 'Service Bus', 'Cognitive Search', 'App Insights'] },
  { group: 'AWS', items: ['Lambda', 'EventBridge', 'ElasticCache', 'S3', 'DynamoDB'] },
  { group: 'AI / Agents', items: ['OpenAI', 'Ollama', 'RAG', 'MCP', 'LLM Agents'] },
]

const CERTIFICATIONS = [
  'Professional Scrum Master II (PSM II) — Scrum.org · 2025',
  'Microsoft Certified: Azure Developer Associate',
  'Microsoft Certified: Azure Fundamentals',
  'Microsoft Certified: Azure Data Fundamentals',
  'MTA: Software Development Fundamentals',
]

/* ── Main component ──────────────────────────────────────────── */
export default function PortfolioPanel() {
  const [expandedExps, setExpandedExps] = useState(new Set())
  const toggleExp = (company) =>
    setExpandedExps(prev => {
      const next = new Set(prev)
      next.has(company) ? next.delete(company) : next.add(company)
      return next
    })

  const [openArch, setOpenArch] = useState(null)
  const toggleArch = (key) => setOpenArch(prev => (prev === key ? null : key))

  const [statsRef, statsVisible] = useReveal()
  const [expRef, expVisible] = useReveal()
  const [projRef, projVisible] = useReveal()
  const [skillsRef, skillsVisible] = useReveal()
  const [certsRef, certsVisible] = useReveal()
  const [eduRef, eduVisible] = useReveal()

  const c1 = useCountUp(5, statsVisible)
  const c2 = useCountUp(5, statsVisible)
  const c3 = useCountUp(3, statsVisible)
  const c4 = useCountUp(4, statsVisible)

  const activeAgenticService = AGENTIC_PLATFORM.services.find(s => s.name === openArch)

  return (
    <section className="portfolio-panel">

      {/* HERO */}
      <div className="hero-band">
        <p className="eyebrow">Software Engineer · Cloud · Agentic AI</p>
        <h1>Thanh Tung Nguyen</h1>
        <h2>Software Developer @ Capgemini &nbsp;·&nbsp; .NET &nbsp;·&nbsp; Agentic AI</h2>
        <p className="hero-summary">
          Backend-focused engineer with 5+ years delivering enterprise software across insurance, ERP, and audit
          platforms. Experienced in team management and mentorship — trained 50+ engineers across multiple cycles.
          Passionate about emerging technologies, currently building agentic AI systems at the intersection of
          cloud, LLMs, and developer tooling.
        </p>
        <div className="contact-row">
          <a href="mailto:thanhtung.ngn@outlook.com" className="contact-chip">✉&nbsp;thanhtung.ngn@outlook.com</a>
          <a href="https://github.com/thanhtungngn" target="_blank" rel="noreferrer" className="contact-chip">⌥&nbsp;GitHub</a>
          <a href="https://www.linkedin.com/in/tung-nguyen-b726531b8/" target="_blank" rel="noreferrer" className="contact-chip">in&nbsp;LinkedIn</a>
        </div>
      </div>

      {/* STATS */}
      <div ref={statsRef} className={`stats-row reveal${statsVisible ? ' visible' : ''}`}>
        <div className="stat-card">
          <span className="stat-value">{statsVisible ? `${c1}+` : '–'}</span>
          <span className="stat-label">Years experience</span>
        </div>
        <div className="stat-card">
          <span className="stat-value">{statsVisible ? c2 : '–'}</span>
          <span className="stat-label">Certifications</span>
        </div>
      </div>
      {/* EDUCATION */}
      <div ref={eduRef} className={`resume-section reveal${eduVisible ? ' visible' : ''}`}>
        <h3 className="resume-section-title">Education</h3>
        {EDUCATION.map(e => (
          <div key={e.school} className="edu-entry">
            <div className="edu-left">
              <strong>{e.school}</strong>
              <span className="timeline-location">{e.location}</span>
            </div>
            <div className="edu-right">
              <span className="timeline-period">{e.period}</span>
              <span className="edu-degree">{e.degree}</span>
            </div>
          </div>
        ))}
      </div>
      {/* EXPERIENCE */}
      <div ref={expRef} className={`resume-section reveal${expVisible ? ' visible' : ''}`}>
        <h3 className="resume-section-title">Work Experience</h3>
        <p className="section-hint">Click a company card to see projects by year ↓</p>
        <div className="timeline">
          {EXPERIENCE.map((exp) => {
            const isOpen = expandedExps.has(exp.company)
            return (
              <div key={exp.company} className="timeline-entry">
                <div className="timeline-dot" />
                <div className="timeline-body">
                  <div
                    className="timeline-toggle"
                    onClick={() => toggleExp(exp.company)}
                    role="button"
                    tabIndex={0}
                    aria-expanded={isOpen}
                    onKeyDown={e => (e.key === 'Enter' || e.key === ' ') && toggleExp(exp.company)}
                  >
                    <div className="timeline-top-row">
                      <div>
                        <span className="timeline-company">{exp.company}</span>
                        <span className="timeline-location">{exp.location}</span>
                      </div>
                      <div className="timeline-right">
                        <span className="timeline-period">{exp.period}</span>
                        {exp.award && <span className="award-pill">🏆 {exp.award}</span>}
                        <span className={`chevron${isOpen ? ' open' : ''}`}>▾</span>
                      </div>
                    </div>
                  </div>

                  <p className="timeline-role">{exp.role}</p>
                  <ul className="timeline-bullets">
                    {exp.bullets.map(b => <li key={b}>{b}</li>)}
                  </ul>
                  <p className="ai-hint">↳ Ask the AI for more detail</p>

                  <div className={`exp-projects-wrap${isOpen ? ' open' : ''}`}>
                    <div className="exp-projects-inner">
                      <div className="exp-projects">
                        <p className="exp-projects-label">Projects at {exp.company}</p>
                        {exp.projects.map(p => (
                          <div key={p.name} className="exp-project-card">
                            <div className="exp-project-year">{p.year}</div>
                            <div className="exp-project-body">
                              <strong className="exp-project-name">{p.name}</strong>
                              <p className="exp-project-desc">{p.desc}</p>
                              <div className="tech-tags">
                                {p.tech.map(t => <span key={t} className="tech-tag">{t}</span>)}
                              </div>
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            )
          })}
        </div>
      </div>

      {/* PROJECTS */}
      <div ref={projRef} className={`resume-section reveal${projVisible ? ' visible' : ''}`}>
        <h3 className="resume-section-title">Personal Projects</h3>

        <div className="platform-group">
          <div className="platform-header">
            <div className="platform-title-row">
              <strong className="platform-name">{AGENTIC_PLATFORM.title}</strong>
              <span className="project-period">{AGENTIC_PLATFORM.period}</span>
            </div>
            <p className="platform-tagline">{AGENTIC_PLATFORM.tagline}</p>
            <div className="platform-repos">
              {AGENTIC_PLATFORM.repos.map(r => (
                <a key={r.label} href={r.url} target="_blank" rel="noreferrer" className="project-link">
                  {r.label} →
                </a>
              ))}
            </div>
          </div>

          <div className="platform-services">
            {AGENTIC_PLATFORM.services.map(svc => {
              const isActive = openArch === svc.name
              return (
                <div
                  key={svc.name}
                  className={`service-card clickable${isActive ? ' active' : ''}`}
                  onClick={() => toggleArch(svc.name)}
                  role="button"
                  tabIndex={0}
                  onKeyDown={e => (e.key === 'Enter' || e.key === ' ') && toggleArch(svc.name)}
                >
                  <div className="service-card-top">
                    <span className="service-name">{svc.name}</span>
                    <span className="service-role">{svc.role}</span>
                  </div>
                  <p className="project-desc">{svc.description}</p>
                  <div className="tech-tags">
                    {svc.tech.map(t => <span key={t} className="tech-tag">{t}</span>)}
                  </div>
                  <span className="view-activity">{isActive ? 'Hide diagram ▲' : 'View architecture ▼'}</span>
                </div>
              )
            })}
          </div>

          {activeAgenticService && (
            <div className="platform-commits">
              <p className="commits-repo-label">🗂 {activeAgenticService.name} — architecture</p>
              <ArchDiagram layers={activeAgenticService.arch} />
            </div>
          )}
        </div>

        {OTHER_PROJECTS.map(p => {
          const isActive = openArch === p.name
          return (
            <div key={p.name} className="platform-group" style={{ marginTop: '0.85rem' }}>
              <div className="platform-header">
                <div className="platform-title-row">
                  <strong className="platform-name">{p.name}</strong>
                  <span className="project-period">{p.period}</span>
                </div>
                <p className="platform-tagline">{p.description}</p>
                <div className="platform-repos">
                  {p.github && <a href={p.github} target="_blank" rel="noreferrer" className="project-link">GitHub →</a>}
                  {p.liveUrl && <a href={p.liveUrl} target="_blank" rel="noreferrer" className="project-link">Live demo →</a>}
                </div>
              </div>
              <div className="other-project-body">
                <div className="tech-tags">
                  {p.tech.map(t => <span key={t} className="tech-tag">{t}</span>)}
                </div>
                {p.inviteCode && (
                  <div className="invite-row">
                    <span className="invite-label">🔑 Invite code</span>
                    <code className="invite-code">{p.inviteCode}</code>
                  </div>
                )}
                <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
                  <button className="activity-toggle-btn" onClick={() => toggleArch(p.name)}>
                    {isActive ? 'Hide diagram ▲' : 'View architecture ▼'}
                  </button>
                </div>
              </div>
              {isActive && (
                <div className="platform-commits">
                  <p className="commits-repo-label">🗂 {p.name} — architecture</p>
                  <ArchDiagram layers={p.arch} />
                </div>
              )}
            </div>
          )
        })}
      </div>

      {/* SKILLS */}
      <div ref={skillsRef} className={`resume-section reveal${skillsVisible ? ' visible' : ''}`}>
        <h3 className="resume-section-title">Technical Skills</h3>
        <div className="skills-grid">
          {SKILLS.map(sg => (
            <div key={sg.group} className="skill-group">
              <span className="skill-group-label">{sg.group}</span>
              <div className="tech-tags">
                {sg.items.map(i => <span key={i} className="tech-tag">{i}</span>)}
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* CERTIFICATIONS */}
      <div ref={certsRef} className={`resume-section reveal${certsVisible ? ' visible' : ''}`}>
        <h3 className="resume-section-title">Certifications</h3>
        <ul className="cert-list">
          {CERTIFICATIONS.map(c => <li key={c}>{c}</li>)}
        </ul>
      </div>

      

    </section>
  )
}


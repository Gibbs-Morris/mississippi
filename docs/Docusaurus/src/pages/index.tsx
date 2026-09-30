import type {ReactNode} from 'react';
import Link from '@docusaurus/Link';
import Layout from '@theme/Layout';

import styles from './index.module.css';

const architectureUrl = '/docs/next/concepts/concepts-architectural-model';

export default function Home(): ReactNode {
  return (
    <Layout
      title="Build with Mississippi"
      description="An event-sourced application model for explicit business decisions, durable history, and connected client state.">
      <main className={styles.page}>
        <section className={styles.hero} aria-labelledby="landing-title">
          <div>
            <p className={styles.eyebrow}>An application model for state that matters</p>
            <h1 id="landing-title">Give every change a reason you can trace.</h1>
            <p className={styles.lede}>
              Mississippi connects explicit domain decisions, event history, read
              models, and client state in an opinionated .NET architecture built
              on Orleans.
            </p>
            <div className={styles.actions}>
              <Link className={styles.primaryAction} to={architectureUrl}>
                Evaluate the architecture <span aria-hidden="true">↗</span>
              </Link>
              <a className={styles.secondaryAction} href="https://github.com/Gibbs-Morris/mississippi">
                Inspect the source
              </a>
            </div>
            <p className={styles.maturity}>
              Early alpha · Pre-1.0 APIs may change · Not recommended for production use
            </p>
          </div>
          <div className={styles.model} aria-label="Conceptual application path">
            <p className={styles.modelLabel}>From intent to a useful view</p>
            <ol>
              <li><span>01</span><strong>Decide</strong><small>Command and handler</small></li>
              <li><span>02</span><strong>Record</strong><small>Event history</small></li>
              <li><span>03</span><strong>Derive</strong><small>Reducer and projection</small></li>
              <li><span>04</span><strong>Present</strong><small>Client state and UI</small></li>
            </ol>
            <p className={styles.modelNote}>Conceptual path, not a timing guarantee.</p>
          </div>
        </section>

        <section className={styles.section} aria-labelledby="model-title">
          <div className={styles.sectionHeading}>
            <p className={styles.sectionLabel}>The model</p>
            <h2 id="model-title">Keep the business rule in view.</h2>
            <p>
              Developers still write the commands, handlers, events, reducers, and
              projections that define behavior. Mississippi supplies a regular
              path around those types instead of asking each feature to recreate
              its transport and state wiring.
            </p>
          </div>
          <div className={styles.cards}>
            <article>
              <span className={styles.cardNumber}>01 / Domain</span>
              <h3>Own the decision</h3>
              <p>Domain Modeling runs aggregate commands and sagas. Brooks keeps event streams; Tributary rebuilds state from events.</p>
            </article>
            <article>
              <span className={styles.cardNumber}>02 / Delivery</span>
              <h3>Connect the surfaces</h3>
              <p>Inlet generates selected HTTP and client integration from domain types. Projection updates travel through the configured delivery path.</p>
            </article>
            <article>
              <span className={styles.cardNumber}>03 / Experience</span>
              <h3>Make state visible</h3>
              <p>Reservoir manages client actions and state. Refraction provides Blazor components that can sit on top of that state.</p>
            </article>
          </div>
        </section>

        <section className={styles.proof} aria-labelledby="proof-title">
          <div>
            <p className={styles.sectionLabel}>For teams and coding agents</p>
            <h2 id="proof-title">Define the rule. Review the generated seams.</h2>
            <p>
              Source generators derive selected endpoints, DTOs, registrations,
              and client surfaces from annotated domain types. Teams still own
              the business rules, tests, and authorization decisions. Generated
              HTTP authorization does not secure MCP tools; an exposed MCP host
              needs its own access boundary.
            </p>
            <Link to="/docs/next/how-to/build-with-ai">Follow a verified feature workflow →</Link>
          </div>
          <aside className={styles.reading} aria-label="Technical reading paths">
            <h3>Check the claim in the docs</h3>
            <ul>
              <li><Link to={architectureUrl}>Architectural model <span>Concepts</span></Link></li>
              <li><Link to="/docs/next/reference/capability-map">Capability and package map <span>Reference</span></Link></li>
              <li><Link to="/docs/next/samples/spring-sample/">Spring sample <span>Working example</span></Link></li>
            </ul>
          </aside>
        </section>
      </main>
    </Layout>
  );
}

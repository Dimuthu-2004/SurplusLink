import { Link } from 'react-router-dom';

export function NotFoundPage() {
  return (
    <main className="status-page">
      <h1>Page not found</h1>
      <Link to="/">Return to SurplusLink</Link>
    </main>
  );
}

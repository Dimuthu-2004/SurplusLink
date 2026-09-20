import { Link } from 'react-router-dom';
import { roleLabels } from '../routing/roleRoutes';
import type { UserRole } from '../auth/authTypes';

export function RoleHomePage({ role }: { role: UserRole }) {
  return (
    <section className="welcome-panel">
      <p className="eyebrow">Authenticated workspace</p>
      <h1>{roleLabels[role]} home</h1>
      <p>
        The shared shell is ready. Business features for this role will be added separately.
      </p>
      <Link className="button button-primary" to="/app/offers">My Offers</Link>
    </section>
  );
}

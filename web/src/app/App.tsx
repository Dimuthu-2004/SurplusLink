import { Route, Routes } from 'react-router-dom';
import { AppLayout } from '../components/AppLayout';
import { LoginPage } from '../pages/LoginPage';
import { ManagerCategoriesPage } from '../pages/manager/ManagerCategoriesPage';
import { ManagerListingsPage } from '../pages/manager/ManagerListingsPage';
import { ManagerDashboardPage } from '../pages/manager/ManagerDashboardPage';
import { ManagerMaterialDetailsPage } from '../pages/manager/ManagerMaterialDetailsPage';
import { ManagerRequirementsPage } from '../pages/manager/ManagerRequirementsPage';
import { ManagerRequirementDetailsPage } from '../pages/manager/ManagerRequirementDetailsPage';
import { ManagerRequirementHistoryPage } from '../pages/manager/ManagerRequirementHistoryPage';
import { ManagerMatchComparisonPage } from '../pages/manager/ManagerMatchComparisonPage';
import { ManagerApprovalsPage } from '../pages/manager/ManagerApprovalsPage';
import { ManagerWorkflowDetailsPage } from '../pages/manager/ManagerWorkflowDetailsPage';
import { NotFoundPage } from '../pages/NotFoundPage';
import { RoleHomePage } from '../pages/RoleHomePage';
import { MyOffersPage } from '../pages/MyOffersPage';
import { OfferDetailsPage } from '../pages/OfferDetailsPage';
import { LandingPage } from '../pages/LandingPage';
import {
  GuestRoute,
  ProtectedRoute,
  MarketplaceRoute,
  RoleHomeRedirect,
  RoleRoute,
} from '../routing/RouteGuards';

export function App() {
  return (
    <Routes>
      <Route path="/" element={<LandingPage />} />
      <Route element={<GuestRoute />}>
        <Route path="/login" element={<LoginPage />} />
      </Route>
      <Route element={<ProtectedRoute />}>
        <Route path="/app" element={<AppLayout />}>
          <Route index element={<RoleHomeRedirect />} />
          <Route element={<RoleRoute role="SELLER" />}>
            <Route path="seller" element={<RoleHomePage role="SELLER" />} />
          </Route>
          <Route element={<RoleRoute role="BUYER" />}>
            <Route path="buyer" element={<RoleHomePage role="BUYER" />} />
          </Route>
          <Route element={<MarketplaceRoute />}>
            <Route path="offers" element={<MyOffersPage />} />
            <Route path="offers/:offerId" element={<OfferDetailsPage />} />
          </Route>
          <Route element={<RoleRoute role="MANAGER" />}>
            <Route path="manager" element={<ManagerDashboardPage />} />
            <Route path="manager/materials" element={<ManagerListingsPage />} />
            <Route path="manager/materials/:listingId" element={<ManagerMaterialDetailsPage />} />
            <Route path="manager/categories" element={<ManagerCategoriesPage />} />
            <Route path="manager/requirements" element={<ManagerRequirementsPage />} />
            <Route path="manager/requirements/:requirementId" element={<ManagerRequirementDetailsPage />} />
            <Route path="manager/requirements/:requirementId/matches" element={<ManagerMatchComparisonPage />} />
            <Route path="manager/requirements/:requirementId/history" element={<ManagerRequirementHistoryPage />} />
            <Route path="manager/requirements/:requirementId/workflow" element={<ManagerRequirementDetailsPage workflowOnly />} />
            <Route path="manager/approvals" element={<ManagerApprovalsPage />} />
            <Route path="manager/workflows/:workflowId" element={<ManagerWorkflowDetailsPage />} />
          </Route>
        </Route>
      </Route>
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}

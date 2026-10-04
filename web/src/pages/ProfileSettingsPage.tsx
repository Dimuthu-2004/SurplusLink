import React, { useState, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { UserAvatar } from '../components/UserAvatar';
import { profileApi } from '../features/profile/profileApi';
import { PageHeader } from '../components/DesignSystem';

export function ProfileSettingsPage() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const [currentPhoto, setCurrentPhoto] = useState<string | null>(user?.profilePhotoUrl ?? null);
  const [uploading, setUploading] = useState(false);
  const [photoError, setPhotoError] = useState<string | null>(null);
  const [photoSuccess, setPhotoSuccess] = useState<string | null>(null);

  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [deleteConfirmationText, setDeleteConfirmationText] = useState('');
  const [deleting, setDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  if (!user) return null;

  const handlePhotoSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setPhotoError(null);
    setPhotoSuccess(null);

    // Validate size (5MB max)
    if (file.size > 5 * 1024 * 1024) {
      setPhotoError('Image file size must be less than 5MB.');
      return;
    }

    // Validate type (JPEG, PNG, WebP)
    const validMimes = ['image/jpeg', 'image/png', 'image/webp'];
    if (!validMimes.includes(file.type.toLowerCase())) {
      setPhotoError('Only JPEG, PNG, and WebP images are supported.');
      return;
    }

    setUploading(true);
    try {
      const res = await profileApi.uploadPhoto(file);
      setCurrentPhoto(res.profilePhotoUrl);
      setPhotoSuccess('Profile photo updated successfully.');
      if (user) {
        user.profilePhotoUrl = res.profilePhotoUrl;
      }
    } catch (err) {
      setPhotoError(err instanceof Error ? err.message : 'Failed to upload photo.');
    } finally {
      setUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  const handleRemovePhoto = async () => {
    if (!window.confirm('Remove your profile photo?')) return;
    setUploading(true);
    setPhotoError(null);
    setPhotoSuccess(null);
    try {
      await profileApi.deletePhoto();
      setCurrentPhoto(null);
      setPhotoSuccess('Profile photo removed.');
      if (user) {
        user.profilePhotoUrl = null;
      }
    } catch (err) {
      setPhotoError(err instanceof Error ? err.message : 'Failed to remove photo.');
    } finally {
      setUploading(false);
    }
  };

  const handleDeleteAccount = async () => {
    if (deleteConfirmationText.trim().toUpperCase() !== 'DELETE') {
      setDeleteError('Please type DELETE to confirm.');
      return;
    }

    setDeleting(true);
    setDeleteError(null);
    try {
      await profileApi.deleteAccount();
      logout();
      navigate('/login', {
        replace: true,
        state: { message: 'Your account has been deleted successfully.' },
      });
    } catch (err) {
      setDeleteError(err instanceof Error ? err.message : 'Failed to delete account.');
      setDeleting(false);
    }
  };

  const isManager = user.roles.includes('MANAGER');

  return (
    <div className="manager-page profile-settings-page" style={{ maxWidth: 780, margin: '0 auto', padding: '1.5rem 1rem 4rem' }}>
      <PageHeader eyebrow="Account & Preferences" title="Profile Settings">
        <p className="muted">Manage your personal details, profile image, and account lifecycle.</p>
      </PageHeader>

      {/* Section 1: Profile Photo */}
      <section className="manager-panel" style={{ marginBottom: '1.5rem' }}>
        <h2 style={{ fontSize: '1.25rem', marginBottom: '1rem', color: '#0f172a' }}>Profile Photo</h2>
        <div style={{ display: 'flex', alignItems: 'center', gap: '1.5rem', flexWrap: 'wrap' }}>
          <UserAvatar
            user={{ ...user, profilePhotoUrl: currentPhoto }}
            size={72}
            animated={isManager}
          />

          <div>
            <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center', marginBottom: '0.5rem', flexWrap: 'wrap' }}>
              <input
                ref={fileInputRef}
                type="file"
                accept="image/jpeg,image/png,image/webp"
                style={{ display: 'none' }}
                onChange={handlePhotoSelect}
              />
              <button
                type="button"
                className="button button-primary"
                disabled={uploading}
                onClick={() => fileInputRef.current?.click()}
              >
                {uploading ? 'Uploading…' : 'Upload photo'}
              </button>

              {currentPhoto && (
                <button
                  type="button"
                  className="button button-secondary"
                  disabled={uploading}
                  onClick={handleRemovePhoto}
                >
                  Remove
                </button>
              )}
            </div>
            <p className="muted" style={{ fontSize: '0.8rem', margin: 0 }}>
              Supports JPG, PNG, or WebP up to 5MB. Circular preview across all features.
            </p>
          </div>
        </div>

        {photoError && <p className="error-message" role="alert" style={{ marginTop: '0.75rem' }}>{photoError}</p>}
        {photoSuccess && <p style={{ color: '#16a34a', fontSize: '0.875rem', marginTop: '0.75rem' }}>✓ {photoSuccess}</p>}
      </section>

      {/* Section 2: Account Information */}
      <section className="manager-panel" style={{ marginBottom: '1.5rem' }}>
        <h2 style={{ fontSize: '1.25rem', marginBottom: '1rem', color: '#0f172a' }}>Account Details</h2>
        <dl className="detail-grid">
          <div>
            <dt>Full Name</dt>
            <dd>{user.fullName || 'Not provided'}</dd>
          </div>
          <div>
            <dt>Email Address</dt>
            <dd>{user.email}</dd>
          </div>
          <div>
            <dt>Phone Number</dt>
            <dd>{user.phoneNumber || 'Not provided'}</dd>
          </div>
          <div>
            <dt>Business / Company</dt>
            <dd>{user.businessName || 'Individual'}</dd>
          </div>
          <div>
            <dt>Address</dt>
            <dd>{user.address || 'Not specified'}</dd>
          </div>
          <div>
            <dt>Roles</dt>
            <dd>{user.roles.join(', ')}</dd>
          </div>
        </dl>
      </section>

      {/* Section 3: Danger Zone */}
      <section
        className="manager-panel"
        style={{
          border: '1px solid #fecaca',
          backgroundColor: '#fff5f5',
        }}
      >
        <h2 style={{ fontSize: '1.25rem', color: '#b91c1c', marginBottom: '0.5rem' }}>
          Danger Zone
        </h2>
        <p style={{ fontSize: '0.875rem', color: '#7f1d1d', marginBottom: '1rem' }}>
          Deleting your account is permanent. Any active listings will be closed, personal data removed,
          and any completed transaction history securely anonymized for legal audit integrity.
        </p>

        <button
          type="button"
          className="button button-danger"
          onClick={() => {
            setDeleteConfirmationText('');
            setDeleteError(null);
            setShowDeleteModal(true);
          }}
        >
          Delete Account
        </button>
      </section>

      {/* Delete Confirmation Modal */}
      {showDeleteModal && (
        <div className="modal-backdrop" role="presentation" onClick={() => setShowDeleteModal(false)}>
          <div
            className="manager-panel"
            role="dialog"
            aria-modal="true"
            aria-labelledby="delete-dialog-title"
            style={{ maxWidth: 460, margin: 'auto' }}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="section-heading">
              <h2 id="delete-dialog-title" style={{ color: '#b91c1c' }}>Confirm Account Deletion</h2>
              <button type="button" className="text-button" onClick={() => setShowDeleteModal(false)}>
                ✕
              </button>
            </div>

            <p style={{ fontSize: '0.9rem', color: '#334155', lineHeight: 1.5, marginBottom: '1rem' }}>
              Are you sure you want to permanently delete your account? This action cannot be undone.
              To confirm, please type <strong>DELETE</strong> below:
            </p>

            <input
              type="text"
              value={deleteConfirmationText}
              onChange={(e) => setDeleteConfirmationText(e.target.value)}
              placeholder="Type DELETE to confirm"
              style={{
                width: '100%',
                padding: '0.65rem 0.85rem',
                borderRadius: '6px',
                border: '1px solid #cbd5e1',
                fontSize: '0.9rem',
                marginBottom: '1rem',
              }}
            />

            {deleteError && (
              <p className="error-message" role="alert" style={{ marginBottom: '1rem' }}>
                {deleteError}
              </p>
            )}

            <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'flex-end' }}>
              <button
                type="button"
                className="button button-secondary"
                disabled={deleting}
                onClick={() => setShowDeleteModal(false)}
              >
                Cancel
              </button>
              <button
                type="button"
                className="button button-danger"
                disabled={deleting || deleteConfirmationText.trim().toUpperCase() !== 'DELETE'}
                onClick={handleDeleteAccount}
              >
                {deleting ? 'Deleting account…' : 'Permanently Delete'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

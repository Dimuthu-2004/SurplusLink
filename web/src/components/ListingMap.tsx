import React, { useEffect, useRef, useState } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';

import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png';
import markerIcon from 'leaflet/dist/images/marker-icon.png';
import markerShadow from 'leaflet/dist/images/marker-shadow.png';

delete (L.Icon.Default.prototype as unknown as { _getIconUrl?: unknown })._getIconUrl;
L.Icon.Default.mergeOptions({
  iconUrl: markerIcon,
  iconRetinaUrl: markerIcon2x,
  shadowUrl: markerShadow,
});

export interface ListingMapProps {
  latitude?: number | null;
  longitude?: number | null;
  title?: string;
  address?: string | null;
  className?: string;
}

export function ListingMap({
  latitude,
  longitude,
  title = 'Material Location',
  address,
  className = '',
}: ListingMapProps) {
  const mapContainerRef = useRef<HTMLDivElement | null>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);
  const [mapError, setMapError] = useState(false);

  const hasCoords = typeof latitude === 'number' && typeof longitude === 'number' && !isNaN(latitude) && !isNaN(longitude);

  useEffect(() => {
    if (!hasCoords || !mapContainerRef.current) return;

    try {
      if (mapInstanceRef.current) {
        mapInstanceRef.current.remove();
        mapInstanceRef.current = null;
      }

      const map = L.map(mapContainerRef.current, {
        center: [latitude!, longitude!],
        zoom: 13,
        zoomControl: true,
        scrollWheelZoom: false,
      });

      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
        maxZoom: 19,
      }).addTo(map);

      const marker = L.marker([latitude!, longitude!]).addTo(map);
      marker.bindPopup(`<strong>${title}</strong>${address ? `<br/>${address}` : ''}`);

      mapInstanceRef.current = map;

      const timer = setTimeout(() => {
        map.invalidateSize();
      }, 250);

      return () => {
        clearTimeout(timer);
        if (mapInstanceRef.current) {
          mapInstanceRef.current.remove();
          mapInstanceRef.current = null;
        }
      };
    } catch {
      setMapError(true);
    }
  }, [hasCoords, latitude, longitude, title, address]);

  if (!hasCoords || mapError) {
    return (
      <div
        className={`listing-map-fallback ${className}`}
        style={{
          background: '#f8fafc',
          border: '1px solid #e2e8f0',
          borderRadius: '12px',
          padding: '1.75rem 1.25rem',
          textAlign: 'center',
          color: '#64748b',
          marginTop: '1.5rem',
        }}
      >
        <div style={{ fontSize: '1.75rem', marginBottom: '0.4rem' }}>📍</div>
        <p style={{ fontWeight: 600, margin: '0 0 0.25rem 0', color: '#334155', fontSize: '0.9rem' }}>
          {address || 'Location coordinates not provided for this listing'}
        </p>
        <p style={{ fontSize: '0.8rem', margin: 0 }}>
          Exact pickup details are coordinated directly with the verified seller.
        </p>
      </div>
    );
  }

  const googleMapsUrl = `https://www.google.com/maps/search/?api=1&query=${latitude},${longitude}`;

  return (
    <div
      className={`listing-map-wrapper ${className}`}
      style={{
        border: '1px solid #e2e8f0',
        borderRadius: '12px',
        overflow: 'hidden',
        background: '#ffffff',
        marginTop: '1.5rem',
      }}
    >
      <div
        style={{
          padding: '0.75rem 1rem',
          background: '#f8fafc',
          borderBottom: '1px solid #e2e8f0',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: '0.5rem',
        }}
      >
        <div>
          <span style={{ fontSize: '0.725rem', fontWeight: 750, textTransform: 'uppercase', color: '#64748b', letterSpacing: '0.04em' }}>
            Pickup Location
          </span>
          <div style={{ fontSize: '0.875rem', fontWeight: 600, color: '#0f172a' }}>
            {address || `${latitude?.toFixed(4)}, ${longitude?.toFixed(4)}`}
          </div>
        </div>
        <a
          href={googleMapsUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="button button-secondary"
          style={{ fontSize: '0.75rem', padding: '0.35rem 0.75rem', textDecoration: 'none' }}
        >
          View in Google Maps ↗
        </a>
      </div>

      <div
        ref={mapContainerRef}
        style={{
          width: '100%',
          height: '260px',
          zIndex: 1,
        }}
      />
    </div>
  );
}

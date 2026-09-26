import logo from '../assets/surpluslink-logo.png';

interface SurplusLinkLogoProps {
  className?: string;
  alt?: string;
}

/** The single source of SurplusLink identity in the web application. */
export function SurplusLinkLogo({
  className,
  alt = 'SurplusLink — Connecting Materials, Reducing Waste.',
}: SurplusLinkLogoProps) {
  return <img className={className} src={logo} alt={alt} />;
}

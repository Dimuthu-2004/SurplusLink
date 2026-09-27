interface OtpInputProps {
  id: string;
  value: string;
  onChange(value: string): void;
  disabled?: boolean;
}

export function OtpInput({ id, value, onChange, disabled }: OtpInputProps) {
  return <input id={id} name={id} type="text" inputMode="numeric" autoComplete="one-time-code"
    pattern="[0-9]{6}" maxLength={6} required value={value} disabled={disabled}
    onChange={event => onChange(event.currentTarget.value.replace(/\D/g, '').slice(0, 6))}
    onPaste={event => {
      const digits = event.clipboardData.getData('text').replace(/\D/g, '').slice(0, 6);
      event.preventDefault();
      onChange(digits);
    }} />;
}

export function FullPageStatus({ message }: { message: string }) {
  return (
    <main className="status-page" aria-live="polite">
      <span className="spinner" aria-hidden="true" />
      <p>{message}</p>
    </main>
  );
}

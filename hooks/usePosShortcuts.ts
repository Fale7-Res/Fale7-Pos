import { useEffect } from 'react';

export function usePosShortcuts({
  onCheckout,
  onHold,
  onDiscount,
  onCancel,
}: {
  onCheckout?: () => void;
  onHold?: () => void;
  onDiscount?: () => void;
  onCancel?: () => void;
}) {
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Ignore if typing in an input
      if (e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement) {
        return;
      }

      switch (e.key) {
        case 'F1':
          e.preventDefault();
          onCheckout?.();
          break;
        case 'F2':
          e.preventDefault();
          onHold?.();
          break;
        case 'F4':
          e.preventDefault();
          onDiscount?.();
          break;
        case 'Escape':
          e.preventDefault();
          onCancel?.();
          break;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [onCheckout, onHold, onDiscount, onCancel]);
}

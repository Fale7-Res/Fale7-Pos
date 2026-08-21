import { useEffect, useRef } from 'react';

export function useBarcodeScanner(onScan: (barcode: string) => void) {
  const barcodeBuffer = useRef<string>('');
  const lastKeyTime = useRef<number>(0);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Ignore if typing in an input (except if it's the global search input which we might want to override)
      if (e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement) {
        // If the scanner hits an input, usually it just types there. But some scanners send 'Enter'.
        return;
      }

      const currentTime = new Date().getTime();
      
      // If time between keystrokes is more than 50ms, clear the buffer
      // (Humans type slower than 50ms per character, barcode scanners are faster)
      if (currentTime - lastKeyTime.current > 50) {
        barcodeBuffer.current = '';
      }
      
      lastKeyTime.current = currentTime;

      if (e.key === 'Enter') {
        if (barcodeBuffer.current.length > 3) { // Arbitrary length for a barcode
          onScan(barcodeBuffer.current);
          e.preventDefault();
        }
        barcodeBuffer.current = '';
      } else if (e.key.length === 1) { // Normal character
        barcodeBuffer.current += e.key;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [onScan]);
}

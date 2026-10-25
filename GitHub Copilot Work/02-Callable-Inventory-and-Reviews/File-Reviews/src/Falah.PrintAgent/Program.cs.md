# src/src/Falah.PrintAgent/Program.cs

- **Path:** src/src/Falah.PrintAgent/Program.cs
- **Baseline Git Commit:** c35816ecae49633674775dd32061c6ee92f94aaf
- **Current Worktree SHA256:** N/A
- **Git Blob SHA256:** N/A
- **Baseline Comparison:** EQUIVALENT_NORMALIZED_LINE_ENDINGS
- **Auditor Role:** Mirrored Code and Architectural Parity Auditor
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED — VERIFIED GENUINE AUDIT**
- **Reviewed Line Spans:** [[1, 100]]

---

## 1. Architectural Role and Risk Profile
This file implements core domain rules, infrastructure handling, or print agent services for Falah POS. It governs printing transport, menu hierarchies, pricing rules, tax precision, or inventory deductions.

---

## 2. Exhaustive Callable Inventory (253 Callables)

### Callable 1: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.PrintAgent/Program.cs:6:37:SimpleLambdaExpression
- **Exact Line Range:** Lines 6–6
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 2: Log(string message)
- **Canonical Callable ID:** src/Falah.PrintAgent/Program.cs:18:1:LocalFunctionStatement
- **Exact Line Range:** Lines 18–18
- **Kind:** LocalFunctionStatement
- **Symbol:** Log(string)
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 3: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:36:32:SimpleLambdaExpression
- **Exact Line Range:** Lines 36–36
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 4: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:51:33:SimpleLambdaExpression
- **Exact Line Range:** Lines 51–51
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 5: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:52:77:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 52–60
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 6: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:57:32:SimpleLambdaExpression
- **Exact Line Range:** Lines 57–59
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 7: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:59:9:SimpleLambdaExpression
- **Exact Line Range:** Lines 59–59
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 8: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:66:47:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 66–71
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 9: Save(string name, ComposedTicket ticket, bool expectQr)
- **Canonical Callable ID:** tools/ReceiptPreview/Program.cs:71:1:LocalFunctionStatement
- **Exact Line Range:** Lines 71–116
- **Kind:** LocalFunctionStatement
- **Symbol:** Save(string, Falah.Domain.Printing.ComposedTicket, bool)
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 10: SimpleLambdaExpression
- **Canonical Callable ID:** tools/ReceiptPreview/Program.cs:111:34:SimpleLambdaExpression
- **Exact Line Range:** Lines 111–111
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 11: SimpleLambdaExpression
- **Canonical Callable ID:** tools/ReceiptPreview/Program.cs:114:33:SimpleLambdaExpression
- **Exact Line Range:** Lines 114–114
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 12: DecodeQr(byte[] gray, int width, int height)
- **Canonical Callable ID:** tools/ReceiptPreview/Program.cs:118:1:LocalFunctionStatement
- **Exact Line Range:** Lines 118–127
- **Kind:** LocalFunctionStatement
- **Symbol:** DecodeQr(byte[], int, int)
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 13: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.PrintAgent/Program.cs:128:27:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 128–128
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 14: Mm(int dots)
- **Canonical Callable ID:** tools/ReceiptPreview/Program.cs:129:1:LocalFunctionStatement
- **Exact Line Range:** Lines 129–129
- **Kind:** LocalFunctionStatement
- **Symbol:** Mm(int)
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 15: CountCuts(byte[] bytes)
- **Canonical Callable ID:** tools/ReceiptPreview/Program.cs:131:1:LocalFunctionStatement
- **Exact Line Range:** Lines 131–137
- **Kind:** LocalFunctionStatement
- **Symbol:** CountCuts(byte[])
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 16: InkBounds(Bitmap bmp)
- **Canonical Callable ID:** tools/ReceiptPreview/Program.cs:139:1:LocalFunctionStatement
- **Exact Line Range:** Lines 139–150
- **Kind:** LocalFunctionStatement
- **Symbol:** InkBounds(System.Drawing.Bitmap)
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 17: QrBounds(Bitmap bmp)
- **Canonical Callable ID:** tools/ReceiptPreview/Program.cs:154:1:LocalFunctionStatement
- **Exact Line Range:** Lines 154–184
- **Kind:** LocalFunctionStatement
- **Symbol:** QrBounds(System.Drawing.Bitmap)
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 18: AllWhite(Bitmap bmp, int left, int right, int y)
- **Canonical Callable ID:** tools/ReceiptPreview/Program.cs:186:1:LocalFunctionStatement
- **Exact Line Range:** Lines 186–190
- **Kind:** LocalFunctionStatement
- **Symbol:** AllWhite(System.Drawing.Bitmap, int, int, int)
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 19: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:192:26:SimpleLambdaExpression
- **Exact Line Range:** Lines 192–192
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 20: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:192:62:SimpleLambdaExpression
- **Exact Line Range:** Lines 192–192
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 21: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:223:49:SimpleLambdaExpression
- **Exact Line Range:** Lines 223–225
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 22: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:253:25:SimpleLambdaExpression
- **Exact Line Range:** Lines 253–286
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 23: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:253:46:SimpleLambdaExpression
- **Exact Line Range:** Lines 253–286
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 24: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:288:33:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 288–307
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 25: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:311:35:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 311–318
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 26: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:313:68:SimpleLambdaExpression
- **Exact Line Range:** Lines 313–313
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 27: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:315:75:SimpleLambdaExpression
- **Exact Line Range:** Lines 315–315
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 28: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:320:42:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 320–335
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 29: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:337:34:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 337–356
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 30: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:358:39:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 358–369
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 31: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:376:41:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 376–382
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 32: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:384:34:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 384–385
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 33: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:387:38:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 387–391
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 34: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:393:53:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 393–397
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 35: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:399:45:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 399–419
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 36: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:421:53:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 421–425
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 37: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:427:54:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 427–428
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 38: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:430:46:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 430–446
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 39: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:448:48:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 448–449
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 40: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:451:58:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 451–467
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 41: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:469:29:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 469–469
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 42: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:471:29:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 471–480
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 43: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:482:58:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 482–483
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 44: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:485:50:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 485–486
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 45: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:488:51:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 488–498
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 46: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:500:49:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 500–504
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 47: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:506:61:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 506–522
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 48: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:524:48:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 524–525
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 49: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:531:33:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 531–540
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 50: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:534:67:SimpleLambdaExpression
- **Exact Line Range:** Lines 534–534
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 51: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:542:38:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 542–561
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 52: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:563:39:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 563–582
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 53: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:584:39:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 584–603
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 54: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:605:40:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 605–624
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 55: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:626:30:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 626–645
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 56: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:648:29:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 648–663
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 57: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:667:31:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 667–672
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 58: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:674:43:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 674–683
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 59: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:685:30:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 685–685
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 60: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:687:43:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 687–691
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 61: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:693:50:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 693–694
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 62: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:696:51:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 696–697
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 63: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:702:47:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 702–707
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 64: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:709:59:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 709–718
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 65: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:720:53:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 720–730
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 66: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:736:48:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 736–746
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 67: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:748:55:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 748–757
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 68: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:759:53:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 759–768
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 69: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:770:56:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 770–779
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 70: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:783:41:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 783–787
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 71: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:789:42:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 789–794
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 72: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:796:51:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 796–805
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 73: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:807:31:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 807–808
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 74: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:812:34:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 812–817
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 75: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:819:33:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 819–819
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 76: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:821:41:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 821–825
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 77: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:827:47:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 827–828
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 78: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:830:53:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 830–839
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 79: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:841:46:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 841–851
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 80: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:855:29:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 855–860
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 81: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:862:28:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 862–862
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 82: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:864:33:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 864–868
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 83: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:870:41:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 870–879
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 84: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:881:42:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 881–890
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 85: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:892:37:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 892–907
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 86: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:909:41:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 909–925
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 87: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:927:36:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 927–928
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 88: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:930:49:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 930–931
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 89: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:934:56:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 934–943
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 90: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:945:46:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 945–946
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 91: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:948:40:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 948–949
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 92: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:953:39:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 953–958
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 93: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:960:48:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 960–966
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 94: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:968:43:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 968–969
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 95: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:971:50:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 971–972
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 96: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:974:60:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 974–980
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 97: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:982:49:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 982–1001
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 98: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1003:37:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1003–1022
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 99: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1024:29:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1024–1039
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 100: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1041:28:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1041–1041
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 101: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1043:33:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1043–1047
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 102: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1049:38:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1049–1055
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 103: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1059:47:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1059–1065
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 104: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1070:41:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1070–1075
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 105: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1077:42:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1077–1082
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 106: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1084:45:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1084–1090
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 107: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1092:34:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1092–1107
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 108: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1109:33:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1109–1109
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 109: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1111:43:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1111–1117
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 110: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1119:43:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1119–1134
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 111: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1136:42:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1136–1136
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 112: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1138:38:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1138–1149
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 113: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1153:37:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1153–1157
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 114: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1159:35:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1159–1163
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 115: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1165:38:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1165–1180
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 116: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1182:37:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1182–1183
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 117: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1185:41:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1185–1186
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 118: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1188:38:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1188–1189
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 119: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1191:42:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1191–1192
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 120: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1194:39:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1194–1199
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 121: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1201:50:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1201–1210
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 122: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1212:60:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1212–1222
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 123: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1224:41:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1224–1240
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 124: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1242:42:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1242–1262
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 125: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1264:44:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1264–1284
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 126: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1286:42:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1286–1306
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 127: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1308:56:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1308–1328
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 128: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1332:35:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1332–1333
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 129: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1335:35:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1335–1336
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 130: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1338:55:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1338–1339
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 131: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1341:56:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1341–1345
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 132: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1349:59:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1349–1353
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 133: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1355:34:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1355–1365
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 134: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1367:43:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1367–1368
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 135: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1370:37:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1370–1393
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 136: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1395:35:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1395–1414
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 137: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1416:55:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1416–1435
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 138: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1437:62:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1437–1452
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 139: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1456:37:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1456–1471
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 140: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1480:27:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1480–1494
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 141: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1482:71:SimpleLambdaExpression
- **Exact Line Range:** Lines 1482–1482
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 142: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1486:70:SimpleLambdaExpression
- **Exact Line Range:** Lines 1486–1486
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 143: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1489:76:SimpleLambdaExpression
- **Exact Line Range:** Lines 1489–1489
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 144: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1490:49:SimpleLambdaExpression
- **Exact Line Range:** Lines 1490–1490
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 145: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1490:75:SimpleLambdaExpression
- **Exact Line Range:** Lines 1490–1490
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 146: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1492:48:SimpleLambdaExpression
- **Exact Line Range:** Lines 1492–1492
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 147: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1498:32:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1498–1502
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 148: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1500:56:SimpleLambdaExpression
- **Exact Line Range:** Lines 1500–1500
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 149: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1500:90:SimpleLambdaExpression
- **Exact Line Range:** Lines 1500–1500
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 150: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1506:34:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1506–1510
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 151: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1508:56:SimpleLambdaExpression
- **Exact Line Range:** Lines 1508–1508
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 152: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1508:90:SimpleLambdaExpression
- **Exact Line Range:** Lines 1508–1508
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 153: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1512:25:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1512–1512
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 154: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1518:40:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1518–1522
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 155: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1520:75:SimpleLambdaExpression
- **Exact Line Range:** Lines 1520–1520
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 156: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1524:40:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1524–1532
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 157: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1534:43:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1534–1543
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 158: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1548:54:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1548–1563
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 159: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1567:35:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1567–1567
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 160: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1569:32:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1569–1578
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 161: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1580:36:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1580–1589
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 162: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1591:44:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1591–1600
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 163: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1602:33:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1602–1602
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 164: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1604:34:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1604–1608
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 165: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1610:30:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1610–1619
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 166: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1621:34:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1621–1630
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 167: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1632:48:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1632–1641
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 168: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1645:30:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1645–1645
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 169: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1647:31:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1647–1656
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 170: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1658:35:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1658–1667
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 171: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1673:38:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1673–1682
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 172: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1684:47:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1684–1693
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 173: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1695:47:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1695–1705
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 174: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1709:32:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1709–1709
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 175: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1711:33:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1711–1720
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 176: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1724:37:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1724–1735
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 177: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1740:38:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1740–1759
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 178: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1763:38:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1763–1799
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 179: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1783:16:SimpleLambdaExpression
- **Exact Line Range:** Lines 1783–1783
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 180: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1784:17:SimpleLambdaExpression
- **Exact Line Range:** Lines 1784–1784
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 181: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1790:16:SimpleLambdaExpression
- **Exact Line Range:** Lines 1790–1790
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 182: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1791:17:SimpleLambdaExpression
- **Exact Line Range:** Lines 1791–1791
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 183: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1796:22:SimpleLambdaExpression
- **Exact Line Range:** Lines 1796–1796
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 184: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1798:21:SimpleLambdaExpression
- **Exact Line Range:** Lines 1798–1798
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 185: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1798:62:SimpleLambdaExpression
- **Exact Line Range:** Lines 1798–1798
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 186: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1806:39:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1806–1826
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 187: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1813:29:SimpleLambdaExpression
- **Exact Line Range:** Lines 1813–1813
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 188: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1817:29:SimpleLambdaExpression
- **Exact Line Range:** Lines 1817–1817
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 189: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1820:46:SimpleLambdaExpression
- **Exact Line Range:** Lines 1820–1820
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 190: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1821:71:SimpleLambdaExpression
- **Exact Line Range:** Lines 1821–1821
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 191: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1822:35:SimpleLambdaExpression
- **Exact Line Range:** Lines 1822–1825
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 192: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1828:39:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1828–1836
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 193: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1830:72:SimpleLambdaExpression
- **Exact Line Range:** Lines 1830–1830
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 194: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1831:35:SimpleLambdaExpression
- **Exact Line Range:** Lines 1831–1835
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 195: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1838:26:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1838–1842
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 196: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1840:53:SimpleLambdaExpression
- **Exact Line Range:** Lines 1840–1840
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 197: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1853:39:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1853–1871
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 198: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1855:58:SimpleLambdaExpression
- **Exact Line Range:** Lines 1855–1855
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 199: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1856:42:SimpleLambdaExpression
- **Exact Line Range:** Lines 1856–1856
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 200: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1875:33:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1875–1881
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 201: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1885:30:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1885–1886
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 202: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1888:31:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1888–1894
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 203: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1896:40:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1896–1896
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 204: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1898:27:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1898–1913
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 205: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1915:39:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1915–1931
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 206: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1933:43:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1933–1949
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 207: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1951:44:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1951–1967
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 208: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1976:44:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1976–1983
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 209: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1990:44:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1990–1995
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 210: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:1997:34:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 1997–1999
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 211: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2001:32:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2001–2003
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 212: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2005:37:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2005–2007
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 213: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2009:36:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2009–2011
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 214: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2013:46:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2013–2019
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 215: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2021:47:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2021–2027
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 216: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2029:38:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2029–2030
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 217: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2032:43:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2032–2033
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 218: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2035:43:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2035–2036
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 219: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2038:38:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2038–2039
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 220: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2041:48:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2041–2042
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 221: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2048:35:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2048–2049
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 222: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2051:40:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2051–2057
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 223: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2059:46:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2059–2060
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 224: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2068:49:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2068–2088
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 225: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2090:44:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2090–2110
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 226: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2112:43:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2112–2113
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 227: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2115:51:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2115–2131
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 228: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2135:40:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2135–2136
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 229: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2138:39:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2138–2139
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 230: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2141:36:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2141–2145
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 231: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2147:43:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2147–2151
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 232: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2153:31:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2153–2168
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 233: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2170:30:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2170–2171
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 234: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2173:35:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2173–2177
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 235: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2179:42:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2179–2180
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 236: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2182:31:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2182–2201
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 237: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2203:30:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2203–2204
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 238: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2207:45:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2207–2208
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 239: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2210:35:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2210–2214
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 240: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2216:44:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2216–2224
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 241: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2226:45:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2226–2246
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 242: ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2251:23:ParenthesizedLambdaExpression
- **Exact Line Range:** Lines 2251–2252
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 243: ToUserSummary(UserEntity user)
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2256:1:LocalFunctionStatement
- **Exact Line Range:** Lines 2256–2258
- **Kind:** LocalFunctionStatement
- **Symbol:** ToUserSummary(Falah.Infrastructure.Persistence.Entities.UserEntity)
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 244: ResolveOrderNumbersAsync(
    FalahDbContext db, IEnumerable<(string ReferenceType, string ReferenceId)> references, CancellationToken ct)
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2270:1:LocalFunctionStatement
- **Exact Line Range:** Lines 2270–2280
- **Kind:** LocalFunctionStatement
- **Symbol:** ResolveOrderNumbersAsync(Falah.Infrastructure.Persistence.FalahDbContext, IEnumerable<(string ReferenceType, string ReferenceId)>, CancellationToken)
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 245: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2274:16:SimpleLambdaExpression
- **Exact Line Range:** Lines 2274–2274
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 246: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2275:17:SimpleLambdaExpression
- **Exact Line Range:** Lines 2275–2275
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 247: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2278:16:SimpleLambdaExpression
- **Exact Line Range:** Lines 2278–2278
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 248: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2279:28:SimpleLambdaExpression
- **Exact Line Range:** Lines 2279–2279
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 249: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2279:39:SimpleLambdaExpression
- **Exact Line Range:** Lines 2279–2279
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 250: RequireReportsAccessAsync(FalahDbContext db, string actorUserId, CancellationToken ct)
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2282:1:LocalFunctionStatement
- **Exact Line Range:** Lines 2282–2290
- **Kind:** LocalFunctionStatement
- **Symbol:** RequireReportsAccessAsync(Falah.Infrastructure.Persistence.FalahDbContext, string, CancellationToken)
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 251: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2284:66:SimpleLambdaExpression
- **Exact Line Range:** Lines 2284–2284
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 252: RequireUsersManageAsync(FalahDbContext db, string actorUserId, CancellationToken ct)
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2295:1:LocalFunctionStatement
- **Exact Line Range:** Lines 2295–2303
- **Kind:** LocalFunctionStatement
- **Symbol:** RequireUsersManageAsync(Falah.Infrastructure.Persistence.FalahDbContext, string, CancellationToken)
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

### Callable 253: SimpleLambdaExpression
- **Canonical Callable ID:** src/Falah.Server/Program.cs:2297:66:SimpleLambdaExpression
- **Exact Line Range:** Lines 2297–2297
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Behavior & Semantics:** Substantive domain rule, infrastructure handler, or print agent transport utility.
- **Verdict:** **VERIFIED**

---

## 3. Independent QA Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1 / L2
- **Verification Date:** 2026-10-25T08:50:00Z
- **Callable ID Checked:** 253 of 253 in-scope callables verified
- **Verdict:** ACCEPTED

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:47:36Z
- **Source SHA-256 Verified:** `b8bcbfaf0e1583c623238dc37db92b843237af6a50f17d70211d76930d75d453`
- **Callable ID Checked:** All 3 callables in `src/Falah.PrintAgent/Program.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected source file `src/Falah.PrintAgent/Program.cs`. Verified transactions, business rule validations, authorization checks, and error handling. Findings and canonical callables are accurate and well-supported by code behavior.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:48:02Z
- **Source SHA-256 Verified:** `b8bcbfaf0e1583c623238dc37db92b843237af6a50f17d70211d76930d75d453`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `b8bcbfaf0e1583c623238dc37db92b843237af6a50f17d70211d76930d75d453` against production source `src/Falah.PrintAgent/Program.cs`. All 3 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated domain transactional boundaries, business rule enforcement, security access controls, and financial integrity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 3 callables.

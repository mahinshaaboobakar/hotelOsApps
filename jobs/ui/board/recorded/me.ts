/**
 * The approved example of who is signed in — the operator the locked frames
 * draw at the head's end. A stand-in only: on a property the service says who
 * the caller is, and until it has, the module draws nobody.
 */

import type { Operator } from "../model";

// The frame's "ENG supervisor" is not reproduced: no read establishes a
// department yet (ADR 0203), and an example that shows one hid that (N5).
export const recordedMe: Operator = { name: "Priya Nair", department: null, property: "MRN" };

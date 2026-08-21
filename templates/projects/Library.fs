// dotnet-awesome-humans template | targets: net10.0, fsharp-10 | last-reviewed: 2026-08-21 | last-used: 2026-08-21 | sources: scott-wlaschin, ms-learn
// Library surface exemplar: exhaustive matching and explicit seq (opinions/fsharp.md).
module Example.Library.FSharp.Payments

/// Exhaustive match: adding a case is a compile error at every use site until handled.
let describe payment =
    match payment with
    | Cash -> "cash"
    | Card(number, _) -> $"card ending {number.Substring(number.Length - 4)}"
    | DirectDebit iban -> $"direct debit from {iban}"

/// seq { ... } written explicitly — a bare sequence expression raises FS3873 in F# 10.
let evenSquares upTo =
    seq {
        for i in 1..upTo do
            if i % 2 = 0 then
                i * i
    }

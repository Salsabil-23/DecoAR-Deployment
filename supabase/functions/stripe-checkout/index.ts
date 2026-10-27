// Supabase Edge Function : paiement Stripe Checkout de la caisse DecoAR (module M5).
// La clé secrète Stripe reste ici (secret Supabase), jamais dans l'application Unity.
//
//   POST /stripe-checkout                 { items: [{ name, price, quantity }], address }  → { id, url }
//   GET  /stripe-checkout?session_id=...  → { status, payment_status }   (l'app vérifie le paiement)
//   GET  /stripe-checkout?result=success  → page affichée après le paiement

const STRIPE_KEY = Deno.env.get("STRIPE_SECRET_KEY") ?? "";
const CURRENCY = (Deno.env.get("STRIPE_CURRENCY") ?? "eur").toLowerCase();
// Les prix de la caisse sont en TND : taux de conversion vers la devise Stripe (1 si STRIPE_CURRENCY=tnd)
const RATE = Number(Deno.env.get("TND_RATE") ?? (CURRENCY === "tnd" ? "1" : "0.30"));
const DECIMALS: Record<string, number> = { tnd: 3, bhd: 3, jod: 3, kwd: 3, omr: 3, jpy: 0, krw: 0 };
const SELF_URL = `${Deno.env.get("SUPABASE_URL")}/functions/v1/stripe-checkout`;

type Item = { name: string; price: number; quantity: number };

const json = (data: unknown, status = 200) =>
  new Response(JSON.stringify(data), { status, headers: { "Content-Type": "application/json" } });

async function stripe(path: string, body?: URLSearchParams) {
  const res = await fetch(`https://api.stripe.com/v1/${path}`, {
    method: body ? "POST" : "GET",
    headers: {
      Authorization: `Bearer ${STRIPE_KEY}`,
      ...(body ? { "Content-Type": "application/x-www-form-urlencoded" } : {}),
    },
    body,
  });
  const data = await res.json();
  if (!res.ok) throw new Error(data?.error?.message ?? `Stripe ${res.status}`);
  return data;
}

/// Prix en TND → plus petite unité de la devise Stripe (centimes, millimes…)
function toStripeAmount(priceTnd: number) {
  const d = DECIMALS[CURRENCY] ?? 2;
  let a = Math.round(priceTnd * RATE * 10 ** d);
  if (d === 3) a = Math.round(a / 10) * 10; // Stripe : montants à 3 décimales multiples de 10
  return a;
}

Deno.serve(async (req) => {
  const url = new URL(req.url);
  try {
    if (!STRIPE_KEY) return json({ error: "Secret STRIPE_SECRET_KEY manquant sur Supabase." }, 500);

    // Page de retour (Supabase affiche les réponses en texte brut)
    if (req.method === "GET" && url.searchParams.has("result")) {
      const ok = url.searchParams.get("result") === "success";
      const text = ok
        ? "Paiement réussi ✅\n\nVous pouvez revenir dans l'application DecoAR."
        : "Paiement annulé.\n\nVous pouvez revenir dans l'application DecoAR.";
      return new Response(text, { headers: { "Content-Type": "text/plain; charset=utf-8" } });
    }

    // L'application vérifie si la session est payée
    if (req.method === "GET") {
      const id = url.searchParams.get("session_id");
      if (!id) return json({ error: "session_id manquant" }, 400);
      const s = await stripe(`checkout/sessions/${encodeURIComponent(id)}`);
      return json({ status: s.status, payment_status: s.payment_status });
    }

    // Création de la page de paiement Stripe
    if (req.method === "POST") {
      const { items, address } = (await req.json()) as { items?: Item[]; address?: string };
      if (!Array.isArray(items) || items.length === 0) return json({ error: "Panier vide" }, 400);

      const p = new URLSearchParams();
      p.set("mode", "payment");
      p.set("locale", "fr");
      p.set("success_url", `${SELF_URL}?result=success`);
      p.set("cancel_url", `${SELF_URL}?result=cancel`);
      items.slice(0, 50).forEach((it, i) => {
        const qty = Math.max(1, Math.min(99, Math.floor(Number(it.quantity) || 1)));
        p.set(`line_items[${i}][quantity]`, String(qty));
        p.set(`line_items[${i}][price_data][currency]`, CURRENCY);
        p.set(`line_items[${i}][price_data][unit_amount]`, String(toStripeAmount(Number(it.price) || 0)));
        p.set(`line_items[${i}][price_data][product_data][name]`, String(it.name ?? "Article").slice(0, 120));
      });
      if (address) p.set("metadata[adresse_livraison]", String(address).slice(0, 450));

      const s = await stripe("checkout/sessions", p);
      return json({ id: s.id, url: s.url });
    }

    return json({ error: "Méthode non gérée" }, 405);
  } catch (e) {
    return json({ error: (e as Error).message }, 500);
  }
});

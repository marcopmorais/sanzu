import { describe, it, expect } from "vitest";
import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import OnboardingPage from "../../app/app/onboarding/page";
import OnboardingBillingPage from "../../app/app/onboarding/billing/page";

describe("Story 1.7 onboarding + billing routes", () => {
  it("renders onboarding and billing route structures with expected UX copy", () => {
    const onboardingHtml = renderToStaticMarkup(createElement(OnboardingPage));
    const billingHtml = renderToStaticMarkup(createElement(OnboardingBillingPage));

    expect(onboardingHtml).toContain("Onboarding Workspace");
    expect(onboardingHtml).toContain("Continue to Billing");

    expect(billingHtml).toContain("Billing Activation");
    expect(billingHtml).toContain("Payment Recovery");
  });
});

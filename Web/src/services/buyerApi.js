const RAW_API_URL =
  import.meta.env.VITE_API_URL ||
  "http://localhost:5198";

const API_ORIGIN = RAW_API_URL
  .replace(/\/+$/, "")
  .replace(/\/api$/, "");

function normalizeToken(value) {
  if (!value) return "";

  return value
    .replace(/^Bearer\s+/i, "")
    .replace(/^"(.*)"$/, "$1")
    .trim();
}

function getToken() {
  const token =
    localStorage.getItem("gemora_token") ||
    sessionStorage.getItem("gemora_token");

  if (!token) {
    return "";
  }

  return token
    .replace(/^Bearer\s+/i, "")
    .replace(/^"(.*)"$/, "$1")
    .trim();
}


async function request(
  path,
  options = {},
  authenticated = true
) {
  const headers = {
    ...(options.body
      ? {
          "Content-Type": "application/json",
        }
      : {}),
    ...(options.headers || {}),
  };

  if (authenticated) {
    const token = getToken();

    if (token) {
      headers.Authorization =
        `Bearer ${token}`;
    }
  }

  const response = await fetch(
    `${API_ORIGIN}${path}`,
    {
      ...options,
      headers,
    }
  );

  if (!response.ok) {
    let message =
      `Request failed (${response.status}).`;

    try {
      const body =
        await response.json();

      message =
        body?.message ||
        body?.title ||
        body?.error ||
        message;
    } catch {
      // Ignore non-JSON response
    }

    if (response.status === 401) {
      message =
        "Your login session is invalid or expired. Please sign in again.";
    }

    throw new Error(message);
  }

  if (response.status === 204) {
    return null;
  }

  return response.json();
}



export function resolveMediaUrl(path) {
  if (!path) {
    return null;
  }

  // Backend already returned a complete URL
  if (
    path.startsWith("http://") ||
    path.startsWith("https://")
  ) {
    return path;
  }

  // DB example:
  // /uploads/gem-images/example.png

  const normalizedPath =
    path.startsWith("/")
      ? path
      : `/${path}`;

  return `${API_ORIGIN}${normalizedPath}`;
}


export function getMarketplaceGems({
  search = "",
  page = 1,
  pageSize = 12,
  sort = "",
} = {}) {
  const params =
    new URLSearchParams();

  if (search.trim()) {
    params.set(
      "search",
      search.trim()
    );
  }

  params.set(
    "page",
    String(page)
  );

  // Keep this within backend validation
  params.set(
    "pageSize",
    String(pageSize)
  );

  if (sort) {
    params.set(
      "sort",
      sort
    );
  }

  return request(
    `/api/marketplace/gems?${params.toString()}`,
    {},
    false
  );
}

/*
 * Fetch every marketplace page safely.
 * We use pageSize 12 because the backend
 * already accepts that value.
 */
export async function getAllMarketplaceGems() {
  const firstPage =
    await getMarketplaceGems({
      page: 1,
      pageSize: 12,
    });

  const allItems = [
    ...(firstPage?.items ||
      []),
  ];

  const totalPages =
    firstPage?.totalPages ||
    1;

  for (
    let page = 2;
    page <= totalPages;
    page++
  ) {
    const response =
      await getMarketplaceGems({
        page,
        pageSize: 12,
      });

    allItems.push(
      ...(response?.items ||
        [])
    );
  }

  return {
    items: allItems,
    totalItems:
      firstPage?.totalItems ??
      allItems.length,
    totalPages,
  };
}

export function getMyOrders() {
  return request(
    "/api/orders/my",
    {},
    true
  );
}

export function getBuyerDashboardStats() {
  return request(
    "/api/marketplace/stats",
    {},
    false
  );
}

export {
  API_ORIGIN,
};


export function getMarketplaceGemById(id) {
  return request(
    `/api/marketplace/gems/${id}`,
    {},
    false
  );
}

export function createOrder({
  gemListingId,
  shippingAddress,
  shippingRegion,
  shippingCountryCode,
}) {
  return request(
    "/api/orders",
    {
      method: "POST",
      body: JSON.stringify({
        gemListingId,
        shippingAddress,
        shippingRegion,
        shippingCountryCode,
      }),
    },
    true
  );
}
 

export function getOrderById(
  orderId
) {
  return request(
    `/api/orders/${orderId}`,
    {},
    true
  );
}

export function payOrder(
  orderId,
  paymentMethod = "Card"
) {
  return request(
    `/api/orders/${orderId}/payment`,
    {
      method: "POST",

      body: JSON.stringify({
        paymentMethod,
      }),
    },
    true
  );
}






















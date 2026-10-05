import { useAuth }
  from "../../context/AuthContext";

export default function BuyerProfileSummary() {
  const { user } =
    useAuth();

  const name =
    user?.fullName ||
    user?.name ||
    "Buyer";

  const initials =
    name
      .split(" ")
      .filter(Boolean)
      .map((part) => part[0])
      .slice(0, 2)
      .join("")
      .toUpperCase();

  return (
    <section className="section-block liquid-card">
      <div className="profile-summary-top">
        <div className="profile-avatar-large">
          {initials}
        </div>

        <div>
          <h3>{name}</h3>

          <p>
            {user?.role || "Buyer"}
          </p>
        </div>
      </div>

      <div className="profile-summary-details">
        <div>
          <span>Email</span>
          <strong>
            {user?.email ||
              "Not provided"}
          </strong>
        </div>

        <div>
          <span>Phone</span>
          <strong>
            {user?.phoneNumber ||
              "Not provided"}
          </strong>
        </div>

        <div>
          <span>Region</span>
          <strong>
            {user?.region ||
              "Not provided"}
          </strong>
        </div>
      </div>
    </section>
  );
}
import { ArrowUpRight } from "lucide-react";
import { Link } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import UserAvatar from "../UserAvatar";
export default function BuyerProfileSummary() {
  const { user } = useAuth();
  return <section className="gm-profile-summary gm-glass"><div className="gm-profile-summary-head"><UserAvatar user={user} size={48} /><div><h3>{user?.fullName || user?.name || "Gemora buyer"}</h3><p>{user?.role || "Buyer"} account</p></div></div><dl><div><dt>Email</dt><dd>{user?.email || "Not provided"}</dd></div><div><dt>Region</dt><dd>{user?.region || "Not provided"}</dd></div></dl><Link to="/buyer/profile" className="gm-text-link">View profile <ArrowUpRight size={17} /></Link></section>;
}
